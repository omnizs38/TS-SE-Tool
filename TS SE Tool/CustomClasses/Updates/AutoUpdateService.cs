/*
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            token.ThrowIfCancellationRequested();
            if (release == null || string.IsNullOrEmpty(release.InstallerUrl) || string.IsNullOrEmpty(release.ChecksumsUrl)) return false;
            if (!GitHubReleaseClient.TryParseSemanticTag(release.TagName, out Version version)
                || !Regex.IsMatch(release.InstallerName ?? string.Empty, @"^TS-SE-Tool-[0-9.]+-setup\.exe\z", RegexOptions.CultureInvariant))
                throw new InvalidDataException("The update package has an invalid tag or filename.");
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TS-SE-Tool", "Updates", release.TagName);
            Directory.CreateDirectory(root);
            string installer = Path.Combine(root, Path.GetFileName(release.InstallerName));
            byte[] installerBytes = await GitHubReleaseClient.DownloadAsync(release.InstallerUrl, token).ConfigureAwait(false);
            byte[] checksumBytes = await GitHubReleaseClient.DownloadAsync(release.ChecksumsUrl, token).ConfigureAwait(false);
            string expected = FindExpectedHash(Encoding.UTF8.GetString(checksumBytes), release.InstallerName);
            string actual = ComputeSha256(installerBytes);
            if (string.IsNullOrEmpty(expected) || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
            token.ThrowIfCancellationRequested();
            File.WriteAllBytes(installer, installerBytes);
            lock (Sync)
            {
                token.ThrowIfCancellationRequested();
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

        internal static string FindExpectedHash(string checksums, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string expected = null;
            foreach (string line in (checksums ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match match = Regex.Match(line.Trim(), @"^(?<hash>[a-fA-F0-9]{64})[ \t]+\*?(?<name>.+)$", RegexOptions.CultureInvariant);
                if (!match.Success || !string.Equals(match.Groups["name"].Value, fileName, StringComparison.Ordinal)) continue;
                string hash = match.Groups["hash"].Value;
                // Ambiguous checksums are not a safe basis for installing executable code.
                if (expected != null && !string.Equals(expected, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The update has conflicting SHA-256 checksums.");
                expected = hash;
            }
            return expected;
        }

        private static string ComputeSha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
