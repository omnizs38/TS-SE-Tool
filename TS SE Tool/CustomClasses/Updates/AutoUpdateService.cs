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
        private static string stagedHash;
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
            string verifiedHash = null;
            string temporary = installer + ".download-" + Guid.NewGuid().ToString("N");
            try
            {
                byte[] checksumBytes = await GitHubReleaseClient.DownloadAsync(release.ChecksumsUrl, token).ConfigureAwait(false);
                string expected = FindExpectedHash(Encoding.UTF8.GetString(checksumBytes), release.InstallerName);
                if (string.IsNullOrEmpty(expected)) throw new InvalidDataException("The update manifest does not contain the installer checksum.");
                await GitHubReleaseClient.DownloadToFileAsync(release.InstallerUrl, temporary, token).ConfigureAwait(false);
                string actual;
                using (FileStream file = File.OpenRead(temporary)) actual = Convert.ToHexString(SHA256.HashData(file));
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
                verifiedHash = expected;
                token.ThrowIfCancellationRequested();
                if (File.Exists(installer)) File.Replace(temporary, installer, null);
                else File.Move(temporary, installer);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            lock (Sync)
            {
                token.ThrowIfCancellationRequested();
                stagedInstaller = installer;
                stagedHash = verifiedHash;
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
            string installer; string hash;
            lock (Sync) { installer = stagedInstaller; hash = stagedHash; }
            if (string.IsNullOrEmpty(installer) || !File.Exists(installer)) return;
            try
            {
                // Encoded Unicode avoids cmd.exe percent expansion and ASCII path corruption.
                string path = installer.Replace("'", "''");
                string command = "$ErrorActionPreference='Stop'; Wait-Process -Id " + Environment.ProcessId + " -ErrorAction SilentlyContinue; " +
                    "$installer='" + path + "'; if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne '" + hash + "') { exit 2 }; " +
                    "$run=Start-Process -FilePath $installer -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS' -Wait -PassThru; " +
                    "if ($run.ExitCode -eq 0) { Remove-Item -LiteralPath $installer -Force }; exit $run.ExitCode";
                ProcessStartInfo start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-EncodedCommand");
                start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(command)));
                Process.Start(start);
            }
            catch (Exception exception) { IO_Utilities.ErrorLogWriter("Could not start the staged update: " + exception); }
        }

        internal static string FindExpectedHash(string checksums, string fileName)
        {
            return ReleaseValidation.FindExpectedHash(checksums, fileName);
        }

    }
}
