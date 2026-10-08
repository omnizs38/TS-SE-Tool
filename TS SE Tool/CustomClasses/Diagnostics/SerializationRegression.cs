/* Copyright 2026 omnizs38 and contributors. Licensed under Apache-2.0. */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TS_SE_Tool.Save.Items;

namespace TS_SE_Tool.Diagnostics
{
    // Synthetic fixtures exercise production code; they are not real-game compatibility evidence.
    internal static class SerializationRegression
    {
        private static string[] Lines(string value) => value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
        internal static void Run()
        {
            for (ushort version = 61; version <= 97; version++)
            {
                string infoText = "SiiNunit\n{\nsave_container : _nameless.info {\n name: \"Synthetic\"\n time: 12\n file_time: 34\n version: " + version + "\n dependencies: 0\n future_info: \"keep-me\"\n}\n}";
                SaveFileInfoData info = new SaveFileInfoData(); info.ProcessData(Lines(infoText));
                Require(info.PrintOut().Contains("future_info: \"keep-me\""), "Unknown info field lost.");
                info.Time = 99; SaveFileInfoData infoReload = new SaveFileInfoData(); infoReload.ProcessData(Lines(info.PrintOut()));
                Require(infoReload.Time == 99 && infoReload.Version == version, "Edited info did not survive reload.");
                SiiNunit unit = Fixture(); string initial = unit.PrintOut(version);
                // Feed unknown scalar, array, and block data through the production parser and merge.
                initial = initial.Replace("economy : _nameless.economy {", "economy : _nameless.economy {\n future_scalar: nil\n future_list: 2\n future_list[0]: \"O'Reilly\"\n future_list[1]: \"\\xd0\\x90\"");
                initial = initial.TrimEnd(); initial = initial.Substring(0, initial.Length - 1) + "future_block : _nameless.future {\n future_value: 42\n}\n}";
                SiiNunit parsed = new SiiNunit(Lines(initial)); string noEdit = parsed.PrintOut(version);
                SiiNunit reloaded = new SiiNunit(Lines(noEdit));
                Require(reloaded.PrintOut(version) == noEdit, "Unstable synthetic no-edit serialization v" + version);
                Require(parsed.SiiNitems.Keys.OrderBy(k => k).SequenceEqual(reloaded.SiiNitems.Keys.OrderBy(k => k)), "Block inventory changed v" + version);
                parsed.Bank.money_account = 9876543210L; parsed.Economy.experience_points = 12345;
                string edited = parsed.PrintOut(version); SiiNunit editedReload = new SiiNunit(Lines(edited));
                Require(editedReload.Bank.money_account == 9876543210L && editedReload.Economy.experience_points == 12345, "Edited game values lost v" + version);
                Require(edited.Contains("future_scalar: nil") && edited.Contains("future_list[1]:") && edited.Contains("future_value: 42"), "Unknown game content lost v" + version);
            }
            string profile = "SiiNunit\n{\nuser_profile : _nameless.profile {\n face: 0\n brand: volvo\n map_path: \"/map/europe.mbd\"\n logo: 0\n company_name: \"Synthetic\"\n male: true\n cached_experience: 0\n cached_distance: 0\n user_data: 18\n";
            for (int i = 0; i < 18; i++) profile += " user_data[" + i + "]: " + (i == 17 ? "\"future-Алматы\"" : "\"0\"") + "\n";
            profile += " active_mods: 0\n customization: 0\n cached_stats: 0\n cached_discovery: 0\n version: 6\n online_user_name: \"\"\n online_password: \"\"\n profile_name: \"Synthetic\"\n creation_time: 0\n save_time: 0\n future_profile: nil\n}\n}";
            SaveFileProfileData data = new SaveFileProfileData(); data.ProcessData(Lines(profile));
            data.CachedExperiencePoints = 12345; string output = data.PrintOut();
            SaveFileProfileData second = new SaveFileProfileData(); second.ProcessData(Lines(output));
            Require(second.CachedExperiencePoints == 12345 && output.Contains("user_data[17]: \"future-Алматы\"") && output.Contains("future_profile: nil"), "Extended profile fields lost.");
            var bodies = new Dictionary<string, List<string>> { ["_nameless.test"] = new List<string> { " items: 2", " items[0]: old", " items[1]: removed", " future: nil" } };
            string shrunk = OriginalBlockMerge.Apply("test : _nameless.test {\n items: 1\n items[0]: new\n}", bodies);
            Require(shrunk.Contains("items[0]: new") && !shrunk.Contains("items[1]") && shrunk.Contains("future: nil"), "Array shrink resurrected removed entries.");
        }
        private static SiiNunit Fixture()
        {
            SiiNunit unit = new SiiNunit { EconomyNameless = "_nameless.economy" };
            Economy economy = new Economy(); Player player = new Player();
            // Empty references in object defaults are not valid graph links.
            foreach (object value in new object[] { economy, player })
                foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (property.PropertyType == typeof(string) && (string)property.GetValue(value) == "") property.SetValue(value, "null");
            unit.SiiNitems.Add(unit.EconomyNameless, economy);
            void Add(string name, object value) { unit.SiiNitems.Add("_nameless." + name, value); }
            economy.bank = "_nameless.bank"; Add("bank", new Bank());
            economy.player = "_nameless.player"; Add("player", player);
            economy.game_progress = "_nameless.progress"; Add("progress", new Game_Progress { generic_transports = "_nameless.generic", undamaged_transports = "_nameless.undamaged", clean_transports = "_nameless.clean" });
            Add("generic", new Transport_Data()); Add("undamaged", new Transport_Data()); Add("clean", new Transport_Data());
            economy.event_queue = "_nameless.events"; Add("events", new Economy_event_Queue());
            economy.mail_ctrl = "_nameless.mail"; Add("mail", new Mail_Ctrl());
            economy.oversize_offer_ctrl = "_nameless.offers"; Add("offers", new Oversize_offer_Ctrl());
            economy.delivery_log = "_nameless.deliveries"; Add("deliveries", new Delivery_log());
            economy.police_offence_log = "_nameless.offences"; Add("offences", new Police_offence_Log());
            economy.police_ctrl = "_nameless.police"; Add("police", new Police_Ctrl());
            economy.used_vehicle_assortment = "_nameless.used"; Add("used", new Used_vehicle_Assortment());
            economy.registry = "_nameless.registry"; Add("registry", new Registry());
            economy.bus_job_log = "_nameless.bus"; Add("bus", new Bus_job_Log());
            unit.NamelessControlList.AddRange(unit.SiiNitems.Keys);
            return unit;
        }
    }
}
