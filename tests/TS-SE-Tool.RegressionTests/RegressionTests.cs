using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TS_SE_Tool;
using TS_SE_Tool.Updates;
using TS_SE_Tool.Utilities;

internal static class RegressionTests
{
    private static int passed;
    private static int failed;

    private static int Main()
    {
        Run("atomic creation and UTF-8 without BOM", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "game.sii");
            AtomicFile.WriteAllText(path, "SiiNunit: сохранение");
            Equal("SiiNunit: сохранение", File.ReadAllText(path));
            Equal(false, File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }));
            Equal(1, Directory.GetFiles(root).Length);
        }));
        Run("atomic replacement", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "game.sii");
            File.WriteAllText(path, "original");
            AtomicFile.WriteAllText(path, "replacement");
            Equal("replacement", File.ReadAllText(path));
            Equal(1, Directory.GetFiles(root).Length);
        }));
        Run("serialization failure preserves original and cleans temporary file", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "game.sii");
            File.WriteAllText(path, "original");
            Throws<InvalidOperationException>(() => AtomicFile.Write(path, writer =>
            {
                writer.Write("partial");
                throw new InvalidOperationException("synthetic serialization failure");
            }));
            Equal("original", File.ReadAllText(path));
            Equal(1, Directory.GetFiles(root).Length);
        }));
        Run("locked target preserves original and cleans temporary file", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "game.sii");
            File.WriteAllText(path, "original");
            using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                Throws<IOException>(() => AtomicFile.WriteAllText(path, "replacement"));
            Equal("original", File.ReadAllText(path));
            Equal(1, Directory.GetFiles(root).Length);
        }));
        Run("invalid numeric and timestamp settings preserve defaults and continue", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "config.cfg");
            string text = "JobPickupTime=invalid\nLoopEvery=255\nTimeMultiplier=NaN\nLastUpdateCheck=9223372036854775807\nDistanceMes=mi\nCustomPathGame=ETS2\nCustomPath=C:\\profiles=one\n";
            File.WriteAllText(path, text);
            ProgSettings settings = new ProgSettings();
            DateTime checkedAt = settings.LastUpdateCheck;
            settings.LoadConfigFromFile(path);
            Equal((short)72, settings.JobPickupTime);
            Equal((byte)0, settings.LoopEvery);
            Equal(1.0, settings.TimeMultiplier);
            Equal(checkedAt, settings.LastUpdateCheck);
            Equal("mi", settings.DistanceMes);
            Equal("C:\\profiles=one", settings.CustomPaths["ETS2"].Single());
            Equal(text, File.ReadAllText(path));
        }));
        foreach (string invalid in new[] { "Infinity", "-Infinity", "NaN", "invalid" })
            Run("invalid multiplier " + invalid, () => InTemporaryDirectory(root =>
            {
                string path = Path.Combine(root, "config.cfg");
                File.WriteAllText(path, "TimeMultiplier=" + invalid);
                ProgSettings settings = new ProgSettings();
                settings.LoadConfigFromFile(path);
                Equal(1.0, settings.TimeMultiplier);
            }));
        Run("negative timestamp does not stop later settings", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "config.cfg");
            File.WriteAllText(path, "LastUpdateCheck=-1\nWeightMes=lb");
            ProgSettings settings = new ProgSettings();
            settings.LoadConfigFromFile(path);
            Equal("lb", settings.WeightMes);
        }));
        Run("valid boundaries and invariant-culture config round trip", () => InTemporaryDirectory(root =>
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                ProgSettings original = new ProgSettings
                {
                    JobPickupTime = 384, LoopEvery = 100, TimeMultiplier = 1.25,
                    LastUpdateCheck = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc)
                };
                string path = Path.Combine(root, "config.cfg");
                original.WriteConfigToFile(path);
                ProgSettings loaded = new ProgSettings();
                loaded.LoadConfigFromFile(path);
                Equal(original.JobPickupTime, loaded.JobPickupTime);
                Equal(original.LoopEvery, loaded.LoopEvery);
                Equal(original.TimeMultiplier, loaded.TimeMultiplier);
                Equal(original.LastUpdateCheck.ToUniversalTime(), loaded.LastUpdateCheck.ToUniversalTime());
                Equal(1, Directory.GetFiles(root).Length);
            }
            finally { Thread.CurrentThread.CurrentCulture = previous; }
        }));
        Run("failed config serialization preserves existing file", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "config.cfg");
            File.WriteAllText(path, "original");
            new ProgSettings { CustomPaths = null }.WriteConfigToFile(path);
            Equal("original", File.ReadAllText(path));
            Equal(1, Directory.GetFiles(root).Length);
        }));
        Run("missing config is created", () => InTemporaryDirectory(root =>
        {
            string path = Path.Combine(root, "config.cfg");
            new ProgSettings().LoadConfigFromFile(path);
            Equal(true, File.Exists(path));
        }));
        foreach (string tag in new[] { "v1.62", "v1.62.1", "v1.62.1.0", "V1.62.1" })
            Run("valid semantic tag " + tag, () =>
            {
                Version version;
                Equal(true, GitHubReleaseClient.TryParseSemanticTag(tag, out version));
                Equal(new Version(1, 62, tag == "v1.62" ? 0 : 1, 0), version);
            });
        foreach (string tag in new[] { "v999999999999999999999.62.1", "v1.999999999999999999.0", "v1.62.1-beta", "v-1.62.1", "../v1.62.1", "v1.62.1\n", "", null })
            Run("reject malformed semantic tag " + (tag ?? "null"), () =>
            {
                Version version;
                Equal(false, GitHubReleaseClient.TryParseSemanticTag(tag, out version));
                Equal<Version>(null, version);
            });
        string name = "TS-SE-Tool-1.62.1-setup.exe";
        string hash = new string('a', 64);
        Run("binary checksum exact match", () => Equal(hash, AutoUpdateService.FindExpectedHash(hash + " *" + name + "\r\n", name)));
        Run("text checksum exact match", () => Equal(hash, AutoUpdateService.FindExpectedHash(hash + "  " + name, name)));
        Run("checksum suffix collision rejected", () => Equal<string>(null, AutoUpdateService.FindExpectedHash(hash + " *evil-" + name, name)));
        Run("non-hex checksum rejected", () => Equal<string>(null, AutoUpdateService.FindExpectedHash(new string('z', 64) + " *" + name, name)));
        Run("conflicting checksums rejected", () => Throws<InvalidDataException>(() => AutoUpdateService.FindExpectedHash(hash + " *" + name + "\n" + new string('b', 64) + " *" + name, name)));
        Run("equivalent checksums accepted", () => Equal(hash.ToUpperInvariant(), AutoUpdateService.FindExpectedHash(hash + " *" + name + "\n" + hash.ToUpperInvariant() + " *" + name, name)));
        Run("cancelled update stops before download", () => Throws<OperationCanceledException>(() => AutoUpdateService.DownloadAndScheduleAsync(null, new CancellationToken(true)).GetAwaiter().GetResult()));
        Console.WriteLine("Passed: " + passed + "; failed: " + failed);
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + exception); }
    }

    private static void InTemporaryDirectory(Action<string> test)
    {
        string root = Path.Combine(Path.GetTempPath(), "tsset-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        CultureInfo previous = Thread.CurrentThread.CurrentUICulture;
        try { test(root); }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = previous;
            Directory.Delete(root, true);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name + ".");
    }
}
