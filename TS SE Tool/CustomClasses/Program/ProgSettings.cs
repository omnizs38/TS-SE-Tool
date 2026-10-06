/*
   Copyright 2016-2020 LIPtoH <liptoh.codebase@gmail.com>
   Licensed under the Apache License, Version 2.0 (the "License");
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    class ProgSettings
    {
        public string ProgramVersion { get; set; } = "0.1.0.0";
        public string ProgPrevVersion { get; set; } = "";
        public string Language { get; set; } = "Default";
        public bool ProposeRandom { get; set; } = false;
        public short JobPickupTime { get; set; } = 72;
        public byte LoopEvery { get; set; } = 0;
        public double TimeMultiplier { get; set; } = 1.0;
        public string DistanceMes { get; set; } = "km";
        public string WeightMes { get; set; } = "kg";
        public string CurrencyMesETS2 { get; set; } = "EUR";
        public string CurrencyMesATS { get; set; } = "USD";
        public DateTime LastUpdateCheck { get; set; } = DateTime.Now;
        public Dictionary<string, List<string>> CustomPaths { get; set; } = new Dictionary<string, List<string>>();

        public void LoadConfigFromFile()
        {
            LoadConfigFromFile(Path.Combine(Directory.GetCurrentDirectory(), "config.cfg"));
        }

        internal void LoadConfigFromFile(string config)
        {
            try
            {
                string gameType = "";
                foreach (string rawLine in File.ReadAllLines(config))
                {
                    if (string.IsNullOrWhiteSpace(rawLine) || rawLine.TrimStart().StartsWith("#")) continue;
                    string[] parts = rawLine.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    string tag = parts[0].Trim();
                    string data = parts[1].Trim();
                    switch (tag)
                    {
                        case "ProgramVersion": ProgPrevVersion = data; break;
                        case "Language": Language = data; break;
                        case "JobPickupTime":
                            if (short.TryParse(data, out short pickup) && pickup >= 0 && pickup <= 384)
                                JobPickupTime = pickup;
                            break;
                        case "LoopEvery":
                            if (byte.TryParse(data, out byte loop) && loop <= 100) LoopEvery = loop;
                            break;
                        case "ProposeRandom":
                            if (bool.TryParse(data, out bool random)) ProposeRandom = random;
                            break;
                        case "TimeMultiplier":
                            if (double.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out double multiplier)
                                && !double.IsNaN(multiplier) && !double.IsInfinity(multiplier))
                                TimeMultiplier = Math.Max(0.1, Math.Min(7.0, multiplier));
                            break;
                        case "DistanceMes": DistanceMes = data; break;
                        case "WeightMes": WeightMes = data; break;
                        case "CurrencyMesETS2": CurrencyMesETS2 = data; break;
                        case "CurrencyMesATS": CurrencyMesATS = data; break;
                        case "CustomPathGame": gameType = data; break;
                        case "CustomPath":
                            if (!string.IsNullOrEmpty(gameType))
                            {
                                if (!CustomPaths.ContainsKey(gameType)) CustomPaths.Add(gameType, new List<string>());
                                if (!CustomPaths[gameType].Contains(data)) CustomPaths[gameType].Add(data);
                            }
                            break;
                        case "LastUpdateCheck":
                            if (TryParseFileTime(data, out DateTime checkedAt)) LastUpdateCheck = checkedAt;
                            break;
                    }
                }
                CustomPaths = CustomPaths.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value.Distinct().ToList());
            }
            catch (FileNotFoundException)
            {
                IO_Utilities.LogWriter("Config.cfg file not found. Writing default settings.");
                WriteConfigToFile(config);
            }
            catch (DirectoryNotFoundException)
            {
                IO_Utilities.LogWriter("Config.cfg directory was not found; keeping in-memory settings.");
            }
            catch (IOException exception)
            {
                // A transient read error must never replace the user's existing configuration.
                IO_Utilities.LogWriter("Could not read config.cfg: " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                IO_Utilities.LogWriter("Could not read config.cfg: " + exception.Message);
            }
            ApplyLanguagePreference();
        }

        private static bool TryParseFileTime(string data, out DateTime value)
        {
            value = default(DateTime);
            if (!long.TryParse(data, out long fileTime)) return false;
            try
            {
                value = DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
                return true;
            }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        private void ApplyLanguagePreference()
        {
            CultureInfo selected = CultureInfo.GetCultureInfo("en-US");
            string languageRoot = Path.Combine(Directory.GetCurrentDirectory(), "lang");
            try
            {
                if (!string.Equals(Language, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    selected = CultureInfo.GetCultureInfo(Language);
                }
                else if (Directory.Exists(languageRoot))
                {
                    CultureInfo system = CultureInfo.InstalledUICulture;
                    string match = Directory.GetDirectories(languageRoot)
                        .Select(Path.GetFileName)
                        .FirstOrDefault(x => string.Equals(x, system.Name, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                        match = Directory.GetDirectories(languageRoot).Select(Path.GetFileName)
                            .FirstOrDefault(x => x.StartsWith(system.TwoLetterISOLanguageName + "-", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match))
                    {
                        selected = CultureInfo.GetCultureInfo(match);
                        Language = match;
                    }
                }
            }
            catch
            {
                Language = "Default";
                selected = CultureInfo.GetCultureInfo("en-US");
            }
            Thread.CurrentThread.CurrentUICulture = selected;
        }

        public void WriteConfigToFile()
        {
            WriteConfigToFile(Path.Combine(Directory.GetCurrentDirectory(), "config.cfg"));
        }

        internal void WriteConfigToFile(string config)
        {
            string[] exclude = { "CustomPaths", "ProgPrevVersion", "LastUpdateCheck" };
            try
            {
                AtomicFile.Write(config, writer =>
                {
                    foreach (PropertyInfo property in GetType().GetProperties())
                        if (!exclude.Contains(property.Name))
                        {
                            object value = property.GetValue(this, null);
                            writer.WriteLine(property.Name + "=" + Convert.ToString(value, CultureInfo.InvariantCulture));
                        }
                    writer.WriteLine("LastUpdateCheck=" + LastUpdateCheck.ToFileTimeUtc());
                    foreach (KeyValuePair<string, List<string>> paths in CustomPaths.OrderBy(x => x.Key))
                    {
                        writer.WriteLine("CustomPathGame=" + paths.Key);
                        foreach (string path in paths.Value.Distinct()) writer.WriteLine("CustomPath=" + path);
                    }
                });
            }
            catch
            {
                IO_Utilities.LogWriter("Could not write config.cfg to " + config);
                UpdateStatusBarMessage.ShowStatusMessage(SMStatus.Error, "error_could_not_write_to_file", config);
            }
        }
    }
}
