using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    internal static class UserSettingsMigration
    {
        internal static void ImportIfNeeded()
        {
            try
            {
                string current = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                if (File.Exists(current)) return;
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string company = Path.Combine(local, "omnizs38 and contributors");
                if (!Directory.Exists(company)) return;
                string[] candidates = Directory.GetDirectories(company)
                    .Where(path => Path.GetFileName(path).StartsWith("TS SE Tool", StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(path).StartsWith("TS_SE_Tool", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(path => Directory.GetDirectories(path))
                    .Select(path => Path.Combine(path, "user.config"))
                    .Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
                foreach (string path in candidates)
                {
                    Dictionary<string, bool> values = LegacyPreferences.Read(path);
                    if (values.Count == 0) continue;
                    if (values.TryGetValue("ShowSplashOnStartup", out bool splash)) Properties.Settings.Default.ShowSplashOnStartup = splash;
                    if (values.TryGetValue("CheckUpdatesOnStartup", out bool check)) Properties.Settings.Default.CheckUpdatesOnStartup = check;
                    if (values.TryGetValue("AutoInstallUpdates", out bool install)) Properties.Settings.Default.AutoInstallUpdates = install;
                    Properties.Settings.Default.Save();
                    IO_Utilities.LogWriter("Imported legacy startup/update preferences without changing the original user.config.");
                    break;
                }
            }
            catch (Exception error) { IO_Utilities.ErrorLogWriter("Could not import legacy preferences: " + error.Message); }
        }
    }
}
