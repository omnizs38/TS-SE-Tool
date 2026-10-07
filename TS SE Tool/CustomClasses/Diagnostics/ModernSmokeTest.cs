using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using TS_SE_Tool.Storage;

namespace TS_SE_Tool.Diagnostics
{
    internal static class ModernSmokeTest
    {
        internal static unsafe int Run(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                byte[] plaintext = Encoding.UTF8.GetBytes("SiiNunit\r\n{\r\n}\r\n");
                fixed (byte* input = plaintext)
                    if (FormMain.SIIGetMemoryFormat(input, (uint)plaintext.Length) != 1) throw new InvalidDataException("The x86 native decoder did not recognize plaintext.");
                Properties.Settings.Default.ShowSplashOnStartup = false;
                Properties.Settings.Default.CheckUpdatesOnStartup = false;
                Properties.Settings.Default.AutoInstallUpdates = false;
                if (args.Length > 1)
                {
                    string source = Path.GetFullPath(args[1]);
                    string before = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)));
                    string target = Path.ChangeExtension(source, ".sqlite");
                    SqliteStorage.Ensure(target, DatabaseKind.Profile, "ETS2", "SyntheticProfile", "Synthetic");
                    if (before != Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)))) throw new InvalidDataException("Migration modified the source .sdf.");
                    using (SqliteConnection connection = SqliteStorage.OpenConnection(target))
                    {
                        connection.Open();
                        using (SqliteCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT CityName FROM CitysTable WHERE ID_city=7";
                            if ((string)command.ExecuteScalar() != "O'Reilly–Алматы") throw new InvalidDataException("Legacy Unicode/ID migration failed.");
                            command.CommandText = "SELECT Amount FROM LegacyCustom WHERE ID=3";
                            if (Convert.ToDecimal(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 123.456789m) throw new InvalidDataException("Legacy decimal migration failed.");
                            command.CommandText = "SELECT Payload FROM LegacyCustom WHERE ID=3";
                            byte[] payload = (byte[])command.ExecuteScalar();
                            if (payload.Length != 3 || payload[2] != 255) throw new InvalidDataException("Legacy binary migration failed.");
                            command.CommandText = "SELECT Optional FROM LegacyCustom WHERE ID=3";
                            if (command.ExecuteScalar() != DBNull.Value) throw new InvalidDataException("Legacy null migration failed.");
                        }
                    }
                }
                using (FormMain form = new FormMain())
                {
                    form.CreateControl();
                    Stopwatch timeout = Stopwatch.StartNew();
                    while (form.BackgroundCacheBusy)
                    {
                        if (timeout.Elapsed > TimeSpan.FromMinutes(3)) throw new TimeoutException("Background cache did not complete.");
                        Application.DoEvents(); Thread.Sleep(50);
                    }
                    Application.DoEvents();
                    if (form.BackgroundCacheError != null) throw new InvalidOperationException("Game-reference cache migration failed.", form.BackgroundCacheError);
                    using (OpenPainter.ColorPicker.FormColorPicker color = new OpenPainter.ColorPicker.FormColorPicker(System.Drawing.Color.Transparent))
                        color.CreateControl();
                }
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test-result.txt"), "PASS: .NET 10 WinForms initialization, x86 native decoder, read-only legacy migration, SQLite values and resources.");
                return 0;
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test-result.txt"), exception.ToString());
                return 1;
            }
        }
    }
}
