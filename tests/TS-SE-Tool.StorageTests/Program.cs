using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TS_SE_Tool;
using TS_SE_Tool.Storage;
using TS_SE_Tool.Updates;
using TS_SE_Tool.Utilities;

internal static class Program
{
    private static int failed;
    private static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + error); }
    }
    private static void Check(bool value) { if (!value) throw new Exception("Assertion failed."); }
    private static long Scalar(SqliteConnection connection, string sql)
    { using (SqliteCommand command = connection.CreateCommand()) { command.CommandText = sql; return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture); } }
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "tsset-storage-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            Test("save batch publishes all edits and retains matching binary backups", () =>
            {
                string a = Path.Combine(root, "profile.sii"), b = Path.Combine(root, "info.sii"), c = Path.Combine(root, "game.sii");
                File.WriteAllBytes(a, new byte[] { 0, 255, 8 }); File.WriteAllText(b, "info-before"); File.WriteAllText(c, "game-before");
                SaveFileBatch.Write(new[] { new SaveFileBatch.Entry(a, "profile-after"), new SaveFileBatch.Entry(b, null), new SaveFileBatch.Entry(c, "game-after", SaveFileBatch.Fingerprint(c)) });
                Check(File.ReadAllText(a) == "profile-after" && File.ReadAllText(b) == "info-before" && File.ReadAllText(c) == "game-after");
                Check(File.ReadAllBytes(Path.Combine(root, "profile_backup.sii")).SequenceEqual(new byte[] { 0, 255, 8 }));
                Check(File.ReadAllText(Path.Combine(root, "game_backup.sii")) == "game-before");
                Check(!Directory.GetFiles(root, "*.tmp").Any());
            });
            Test("save batch rolls back after each possible publication failure", () =>
            {
                for (int fault = 1; fault <= 3; fault++)
                {
                    string[] paths = new[] { "profile.sii", "info.sii", "game.sii" }.Select(n => Path.Combine(root, n)).ToArray();
                    foreach (string path in paths) File.WriteAllText(path, "original-Алматы");
                    bool threw = false;
                    try { SaveFileBatch.Write(paths.Select(p => new SaveFileBatch.Entry(p, "edited")).ToArray(), step => { if (step == fault) throw new IOException("Injected failure"); }); }
                    catch (IOException) { threw = true; }
                    Check(threw && paths.All(p => File.ReadAllText(p) == "original-Алматы"));
                    Check(!Directory.GetFiles(root, "*.tmp").Any());
                }
            });
            if (OperatingSystem.IsWindows()) Test("locked second save rolls back first replacement on actual Windows I/O failure", () =>
            {
                string first = Path.Combine(root, "locked-profile.sii"), second = Path.Combine(root, "locked-info.sii");
                File.WriteAllText(first, "original-profile"); File.WriteAllText(second, "original-info"); bool threw = false;
                using (FileStream locked = new FileStream(second, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    try { SaveFileBatch.Write(new[] { new SaveFileBatch.Entry(first, "new-profile"), new SaveFileBatch.Entry(second, "new-info") }); }
                    catch (IOException) { threw = true; }
                }
                Check(threw && File.ReadAllText(first) == "original-profile" && File.ReadAllText(second) == "original-info");
                Check(!Directory.GetFiles(root, "*.tmp").Any());
            });
            Test("changed save with unchanged timestamp is rejected before backups", () =>
            {
                string path = Path.Combine(root, "concurrent.sii"); File.WriteAllText(path, "original"); string hash = SaveFileBatch.Fingerprint(path); DateTime stamp = File.GetLastWriteTimeUtc(path);
                File.WriteAllText(path, "external"); File.SetLastWriteTimeUtc(path, stamp); bool threw = false;
                try { SaveFileBatch.Write(new[] { new SaveFileBatch.Entry(path, "edited", hash) }); } catch (IOException) { threw = true; }
                Check(threw && File.ReadAllText(path) == "external" && !File.Exists(Path.Combine(root, "concurrent_backup.sii")));
            });
            Test("empty serialization stages nothing and never touches original", () =>
            {
                string path = Path.Combine(root, "empty.sii"); File.WriteAllText(path, "keep"); bool threw = false;
                try { SaveFileBatch.Write(new[] { new SaveFileBatch.Entry(path, "") }); } catch (InvalidDataException) { threw = true; }
                Check(threw && File.ReadAllText(path) == "keep" && !Directory.GetFiles(root, "*.tmp").Any());
            });
            Test("incomplete rollback reports recovery and never overwrites external edits", () =>
            {
                string path = Path.Combine(root, "racing.sii"); File.WriteAllText(path, "original"); bool threw = false;
                try { SaveFileBatch.Write(new[] { new SaveFileBatch.Entry(path, "edited") }, step => { File.WriteAllText(path, "external"); throw new IOException("Injected failure"); }); }
                catch (AggregateException e) { threw = e.InnerExceptions.Count == 2; }
                Check(threw && File.ReadAllText(path) == "external" && File.ReadAllText(Path.Combine(root, "racing_backup.sii")) == "original");
            });
            Test("profile schema, metadata, repeated open", () =>
            {
                string path = Path.Combine(root, "profile.sqlite"); SqliteStorage.Ensure(path, DatabaseKind.Profile, "ETS2", "profile", "Алматы"); SqliteStorage.Ensure(path, DatabaseKind.Profile);
                using (SqliteConnection connection = SqliteStorage.OpenConnection(path))
                { connection.Open(); Check(Scalar(connection, "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'") == 19); Check(Scalar(connection, "PRAGMA user_version") == 1); }
            });
            Test("external schema with foreign keys", () =>
            {
                string path = Path.Combine(root, "external.sqlite"); SqliteStorage.Ensure(path, DatabaseKind.External);
                using (SqliteConnection connection = SqliteStorage.OpenConnection(path))
                { connection.Open(); Check(Scalar(connection, "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'") == 7); Check(Scalar(connection, "PRAGMA foreign_keys") == 1); }
            });
            Test("1500 parameterized rows with quotes and Unicode, idempotent inserts", () =>
            {
                using (SqliteConnection connection = SqliteStorage.OpenConnection(Path.Combine(root, "profile.sqlite")))
                using (DataTable rows = new DataTable())
                {
                    rows.Columns.Add("CityName", typeof(string)); for (int i = 0; i < 1500; i++) rows.Rows.Add("city'–Алматы-" + i);
                    SqliteStorage.WriteRows(connection, "CitysTable", rows, true); SqliteStorage.WriteRows(connection, "CitysTable", rows, true);
                    connection.Open(); Check(Scalar(connection, "SELECT count(*) FROM CitysTable") == 1500);
                }
            });
            Test("bulk failure rolls back all rows", () =>
            {
                using (SqliteConnection connection = SqliteStorage.OpenConnection(Path.Combine(root, "profile.sqlite")))
                using (DataTable rows = new DataTable())
                {
                    rows.Columns.Add("CityName", typeof(string)); rows.Rows.Add("new-city"); rows.Rows.Add("city'–Алматы-0");
                    bool threw = false; try { SqliteStorage.WriteRows(connection, "CitysTable", rows); } catch (SqliteException) { threw = true; }
                    Check(threw); connection.Open(); Check(Scalar(connection, "SELECT count(*) FROM CitysTable") == 1500);
                }
            });
            Test("route upsert inserts and updates without duplicates", () =>
            {
                using (SqliteConnection connection = SqliteStorage.OpenConnection(Path.Combine(root, "profile.sqlite")))
                {
                    SqliteStorage.Execute(connection, "INSERT INTO CompaniesTable (CompanyName) VALUES ('company')");
                    string insert = "INSERT INTO DistancesTable (SourceCityID,SourceCompanyID,DestinationCityID,DestinationCompanyID,Distance,FerryTime,FerryPrice) VALUES(1,1,2,1,{0},0,0) ON CONFLICT(SourceCityID,SourceCompanyID,DestinationCityID,DestinationCompanyID) DO UPDATE SET Distance=excluded.Distance";
                    SqliteStorage.Execute(connection, string.Format(CultureInfo.InvariantCulture, insert, 100)); SqliteStorage.Execute(connection, string.Format(CultureInfo.InvariantCulture, insert, 200));
                    Check(Scalar(connection, "SELECT count(*) FROM DistancesTable") == 1); Check(Scalar(connection, "SELECT Distance FROM DistancesTable") == 200);
                }
            });
            Test("dangling foreign keys are rejected", () =>
            {
                using (SqliteConnection connection = SqliteStorage.OpenConnection(Path.Combine(root, "profile.sqlite")))
                { bool threw = false; try { SqliteStorage.Execute(connection, "INSERT INTO CompaniesInCitysTable (CityID,CompanyID) VALUES (999999,1)"); } catch (SqliteException) { threw = true; } Check(threw); }
            });
            Test("legacy XML preserves IDs, quotes, decimals, BLOB, bool and null", () =>
            {
                string path = Path.Combine(root, "import.sqlite"); SqliteStorage.Ensure(path, DatabaseKind.Profile);
                string xml = Path.Combine(root, "export.xml");
                using (DataSet data = new DataSet("LegacyDatabase"))
                {
                    DataTable cities = data.Tables.Add("CitysTable"); cities.Columns.Add("ID_city", typeof(int)); cities.Columns.Add("CityName", typeof(string)); cities.Rows.Add(7, "O'Reilly–Алматы");
                    DataTable custom = data.Tables.Add("LegacyCustom"); custom.Columns.Add("ID", typeof(int)); custom.Columns.Add("Amount", typeof(decimal)); custom.Columns.Add("Payload", typeof(byte[])); custom.Columns.Add("Enabled", typeof(bool)); custom.Columns.Add("Optional", typeof(string));
                    custom.Rows.Add(3, 123456789012.123456m, new byte[] { 0, 255 }, true, DBNull.Value); data.WriteXml(xml, XmlWriteMode.WriteSchema);
                }
                using (SqliteConnection connection = SqliteStorage.OpenConnection(path))
                {
                    connection.Open(); SqliteStorage.ImportXml(connection, xml); Check(Scalar(connection, "SELECT ID_city FROM CitysTable") == 7);
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Amount,Payload,Enabled,Optional FROM LegacyCustom";
                        using (SqliteDataReader reader = command.ExecuteReader()) { Check(reader.Read()); Check(Convert.ToDecimal(reader[0], CultureInfo.InvariantCulture) == 123456789012.123456m); Check(((byte[])reader[1])[1] == 255); Check(Convert.ToInt32(reader[2], CultureInfo.InvariantCulture) == 1); Check(reader[3] == DBNull.Value); }
                    }
                }
            });
            Test("missing exporter leaves original .sdf unchanged and publishes no database", () =>
            {
                string target = Path.Combine(root, "unavailable.sqlite"); string source = Path.ChangeExtension(target, ".sdf"); File.WriteAllText(source, "synthetic legacy data");
                bool threw = false; try { SqliteStorage.Ensure(target, DatabaseKind.Profile, exporter: Path.Combine(root, "missing.exe")); } catch (FileNotFoundException) { threw = true; }
                Check(threw && !File.Exists(target)); Check(File.ReadAllText(source) == "synthetic legacy data"); Check(!Directory.GetFiles(root, "*.migrating-*").Any());
            });
            Test("corrupt SQLite is not recreated", () =>
            {
                string path = Path.Combine(root, "corrupt.sqlite"); File.WriteAllText(path, "not a database"); bool threw = false;
                try { SqliteStorage.Ensure(path, DatabaseKind.Profile); } catch (SqliteException) { threw = true; }
                Check(threw); Check(File.ReadAllText(path) == "not a database");
            });
            Test("broken-reference XML import rolls back", () =>
            {
                string path = Path.Combine(root, "broken-import.sqlite"); SqliteStorage.Ensure(path, DatabaseKind.Profile);
                string xml = Path.Combine(root, "broken-export.xml");
                using (DataSet data = new DataSet("LegacyDatabase"))
                {
                    DataTable links = data.Tables.Add("CompaniesInCitysTable"); links.Columns.Add("ID_CmpnToCt", typeof(int)); links.Columns.Add("CityID", typeof(int)); links.Columns.Add("CompanyID", typeof(int)); links.Rows.Add(1, 123, 456); data.WriteXml(xml, XmlWriteMode.WriteSchema);
                }
                using (SqliteConnection connection = SqliteStorage.OpenConnection(path))
                {
                    connection.Open(); bool threw = false; try { SqliteStorage.ImportXml(connection, xml); } catch (InvalidDataException) { threw = true; }
                    Check(threw); Check(Scalar(connection, "SELECT count(*) FROM CompaniesInCitysTable") == 0); Check(Scalar(connection, "PRAGMA foreign_keys") == 1);
                }
            });
            Test("atomic JSON preference round trip", () =>
            {
                string path = Path.Combine(root, "preferences.json");
                var saved = new TS_SE_Tool.Properties.Settings { ShowSplashOnStartup = false, CheckUpdatesOnStartup = false, AutoInstallUpdates = true };
                saved.Save(path); var loaded = TS_SE_Tool.Properties.Settings.Read(path);
                Check(!loaded.ShowSplashOnStartup && !loaded.CheckUpdatesOnStartup && loaded.AutoInstallUpdates);
            });
            Test("malformed preferences preserved and automatic execution disabled", () =>
            {
                string path = Path.Combine(root, "broken-preferences.json"); File.WriteAllText(path, "not-json");
                Check(!TS_SE_Tool.Properties.Settings.Read(path).AutoInstallUpdates); Check(File.ReadAllText(path) == "not-json");
            });
            Test("legacy preferences preserve disabled updates", () =>
            {
                string path = Path.Combine(root, "user.config"); File.WriteAllText(path, "<configuration><userSettings><TS_SE_Tool.Properties.Settings><setting name='AutoInstallUpdates'><value>False</value></setting><setting name='CheckUpdatesOnStartup'><value>False</value></setting></TS_SE_Tool.Properties.Settings></userSettings></configuration>");
                var preferences = LegacyPreferences.Read(path); Check(!preferences["AutoInstallUpdates"] && !preferences["CheckUpdatesOnStartup"]);
            });
            Test("legacy preference XML rejects DTDs", () =>
            {
                string path = Path.Combine(root, "unsafe.config"); File.WriteAllText(path, "<!DOCTYPE configuration [<!ENTITY value SYSTEM 'file:///nonexistent'>]><configuration>&value;</configuration>");
                bool threw = false; try { LegacyPreferences.Read(path); } catch (System.Xml.XmlException) { threw = true; } Check(threw);
            });
            Test("BCL gzip round trip including Unicode", () =>
            { string value = "positions: Алма-Ата \n"; byte[] zipped = ZipDataUtilities.zipText(value); Check(ZipDataUtilities.unzipText(Convert.ToHexString(zipped)) == value); });
            Test("JSON client ignores prereleases/overflow and untrusted installer URLs", () =>
            {
                object[] releases = {
                    new { tag_name = "v999999999999999.0.0", draft = false, prerelease = false },
                    new { tag_name = "v99.0.0", draft = false, prerelease = true },
                    new { tag_name = "v1.63.0", draft = false, prerelease = false, assets = new[] { new { name = "TS-SE-Tool-1.63.0-setup.exe", browser_download_url = "https://evil.example/setup.exe" } } }
                };
                GitHubReleaseInfo release = GitHubReleaseClient.ParseReleasesJson(JsonSerializer.Serialize(releases)); Check(release.TagName == "v1.63.0" && release.InstallerUrl == null);
                string url = "https://github.com/omnizs38/TS-SE-Tool/releases/download/v1.63.0/TS-SE-Tool-1.63.0-setup.exe";
                release = GitHubReleaseClient.ParseReleasesJson(JsonSerializer.Serialize(new[] { new { tag_name = "v1.63.0", draft = false, prerelease = false, assets = new[] { new { name = "TS-SE-Tool-1.63.0-setup.exe", browser_download_url = url } } } }));
                Check(release.InstallerUrl == url);
            });
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("Storage/compatibility failures: " + failed); return failed == 0 ? 0 : 1;
    }
}
