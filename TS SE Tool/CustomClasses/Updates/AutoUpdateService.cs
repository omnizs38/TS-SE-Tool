/*
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool.Updates
{
    internal static class AutoUpdateService
    {
        private static readonly object Sync = new object();
        private static string stagedInstaller;
        private static bool exitHooked;

        internal static async Task<bool> DownloadAndScheduleAsync(GitHubReleaseInfo release, CancellationToken token)
        {
            if (release == null || string.IsNullOrEmpty(release.InstallerUrl) || string.IsNullOrEmpty(release.ChecksumsUrl)) return false;
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TS-SE-Tool", "Updates", release.TagName);
            Directory.CreateDirectory(root);
            string installer = Path.Combine(root, Path.GetFileName(release.InstallerName));
            byte[] installerBytes = await GitHubReleaseClient.DownloadAsync(release.InstallerUrl, token).ConfigureAwait(false);
            byte[] checksumBytes = await GitHubReleaseClient.DownloadAsync(release.ChecksumsUrl, token).ConfigureAwait(false);
            string expected = FindExpectedHash(Encoding.UTF8.GetString(checksumBytes), release.InstallerName);
            string actual = ComputeSha256(installerBytes);
            if (string.IsNullOrEmpty(expected) || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
            File.WriteAllBytes(installer, installerBytes);
            lock (Sync)
            {
                stagedInstaller = installer;
                if (!exitHooked)
                {
                    Application.ApplicationExit += InstallOnExit;
                    exitHooked = true;
                }
            }
            IO_Utilities.LogWriter("Update " + release.TagName + " downloaded and verified; installation is scheduled for application exit.");
            return true;
        }

        private static void InstallOnExit(object sender, EventArgs e)
        {
            string installer;
            lock (Sync) installer = stagedInstaller;
            if (string.IsNullOrEmpty(installer) || !File.Exists(installer)) return;
            try
            {
                string command = "@echo off\r\ntimeout /t 2 /nobreak >nul\r\nstart \"\" /wait \"" + installer + "\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS\r\ndel /q \"" + installer + "\"\r\ndel /q \"%~f0\"\r\n";
                string script = Path.Combine(Path.GetDirectoryName(installer), "install-update.cmd");
                File.WriteAllText(script, command, Encoding.ASCII);
                Process.Start(new ProcessStartInfo { FileName = script, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
            }
            catch (Exception exception) { IO_Utilities.ErrorLogWriter("Could not start the staged update: " + exception); }
        }

        private static string FindExpectedHash(string checksums, string fileName)
        {
            foreach (string line in (checksums ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (!trimmed.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) continue;
                string hash = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (hash != null && hash.Length == 64) return hash;
            }
            return null;
        }

        private static string ComputeSha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
