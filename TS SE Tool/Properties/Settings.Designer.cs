// Hand-maintained modern preferences; no .NET Framework settings provider.
using System;
using System.IO;
using System.Text.Json;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool.Properties
{
    internal sealed class Settings
    {
        private static readonly Lazy<Settings> Instance = new Lazy<Settings>(Load);
        internal static string PreferencesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TS-SE-Tool", "preferences.json");
        public static Settings Default => Instance.Value;
        public bool ShowSplashOnStartup { get; set; } = true;
        public bool CheckUpdatesOnStartup { get; set; } = true;
        // Do not silently opt users into executing updates if their old preferences cannot be imported.
        public bool AutoInstallUpdates { get; set; } = false;
        public Settings() { }

        private static Settings Load() => Read(PreferencesPath);

        internal static Settings Read(string path)
        {
            if (!File.Exists(path)) return new Settings();
            try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException)
            {
                IO_Utilities.ErrorLogWriter("Could not read preferences.json; existing file retained: " + error.Message);
                return new Settings();
            }
        }

        public void Save() => Save(PreferencesPath);

        internal void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            AtomicFile.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
