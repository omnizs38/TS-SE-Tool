using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Data.Sqlite;

namespace TS_SE_Tool.Storage
{
    internal static class SqliteStorage
    {
        internal static SqliteConnection OpenConnection(string path)
        {
            return new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(path), ForeignKeys = true, Pooling = false,
                Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 30
            }.ToString());
        }

        internal static void Ensure(string path, DatabaseKind kind, string game = "", string profile = "", string readable = "", string exporter = null)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
            {
                using (SqliteConnection existing = OpenConnection(path))
                {
                    existing.Open();
                    using (SqliteCommand check = existing.CreateCommand())
                    {
                        check.CommandText = "PRAGMA quick_check";
                        if (!string.Equals(Convert.ToString(check.ExecuteScalar(), CultureInfo.InvariantCulture), "ok", StringComparison.Ordinal))
                            throw new InvalidDataException("SQLite integrity check failed; original database was not changed.");
                    }
                }
                return;
            }
            string legacy = Path.ChangeExtension(path, ".sdf");
            string staging = path + ".migrating-" + Guid.NewGuid().ToString("N");
            string export = staging + ".xml";
            try
            {
                using (SqliteConnection connection = OpenConnection(staging))
                {
                    connection.Open();
                    Execute(connection, kind == DatabaseKind.Profile ? DatabaseSchema.Profile : DatabaseSchema.External);
                    if (kind == DatabaseKind.Profile)
                    {
                        using (SqliteCommand metadata = connection.CreateCommand())
                        {
                            metadata.CommandText = "INSERT INTO DatabaseDetails (GameName,SaveVersion,ProfileName,V1,V2,V3,V4,ReadableName) VALUES ($game,0,$profile,0,3,6,0,$readable)";
                            metadata.Parameters.AddWithValue("$game", game);
                            metadata.Parameters.AddWithValue("$profile", profile);
                            metadata.Parameters.AddWithValue("$readable", readable);
                            metadata.ExecuteNonQuery();
                        }
                    }
                    if (File.Exists(legacy))
                    {
                        ExportLegacy(legacy, export, exporter);
                        ImportXml(connection, export);
                    }
                    Execute(connection, "PRAGMA user_version=1");
                    using (SqliteCommand foreign = connection.CreateCommand())
                    {
                        foreign.CommandText = "PRAGMA foreign_key_check";
                        using (SqliteDataReader rows = foreign.ExecuteReader())
                            if (rows.Read()) throw new InvalidDataException("Legacy database contains broken references; migration was not committed.");
                    }
                    Execute(connection, "PRAGMA optimize");
                }
                // Publish only a complete, closed, validated database. Never rename/delete the .sdf.
                File.Move(staging, path);
            }
            finally
            {
                foreach (string temporary in new[] { export, staging, staging + "-journal", staging + "-wal", staging + "-shm" })
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        private static void ExportLegacy(string source, string output, string exporter)
        {
            exporter = exporter ?? Path.Combine(AppContext.BaseDirectory, "migration", "LegacySqlCeExport.exe");
            if (!File.Exists(exporter)) throw new FileNotFoundException("The read-only legacy migration helper is missing. No existing data was changed.", exporter);
            ProcessStartInfo start = new ProcessStartInfo(exporter)
            {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(exporter), RedirectStandardError = true, RedirectStandardOutput = true
            };
            start.ArgumentList.Add(source);
            start.ArgumentList.Add(output);
            using (Process process = Process.Start(start))
            {
                if (process == null) throw new IOException("Could not start the legacy database exporter.");
                System.Threading.Tasks.Task<string> errors = process.StandardError.ReadToEndAsync();
                System.Threading.Tasks.Task<string> outputText = process.StandardOutput.ReadToEndAsync();
                if (!process.WaitForExit(300000))
                {
                    process.Kill(true);
                    process.WaitForExit();
                    throw new TimeoutException("Legacy migration exceeded five minutes; existing data was not changed.");
                }
                if (process.ExitCode != 0 || !File.Exists(output)) throw new InvalidDataException("Legacy database export failed; existing data was not changed. " + errors.GetAwaiter().GetResult());
            }
        }

        internal static void ImportXml(SqliteConnection connection, string path)
        {
            using (DataSet exported = new DataSet())
            {
                using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    exported.ReadXml(reader, XmlReadMode.ReadSchema);
                Execute(connection, "PRAGMA foreign_keys=OFF");
                try
                {
                    using (SqliteTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (DataTable table in exported.Tables)
                        {
                            using (SqliteCommand exists = connection.CreateCommand())
                            {
                                exists.Transaction = transaction;
                                exists.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=$name";
                                exists.Parameters.AddWithValue("$name", table.TableName);
                                if (Convert.ToInt64(exists.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                                {
                                    // Preserve unknown/custom legacy tables instead of silently discarding them.
                                    string columns = string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Quote(c.ColumnName) + " " + Affinity(c.DataType)));
                                    using (SqliteCommand create = connection.CreateCommand())
                                    {
                                        create.Transaction = transaction;
                                        create.CommandText = "CREATE TABLE " + Quote(table.TableName) + " (" + columns + ")";
                                        create.ExecuteNonQuery();
                                    }
                                }
                            }
                            if (table.TableName == "DatabaseDetails")
                            {
                                foreach (DataRow row in table.Rows)
                                {
                                    foreach (string column in new[] { "SaveVersion", "GameName", "ProfileName", "ReadableName" })
                                    {
                                        if (!table.Columns.Contains(column) || row[column] == DBNull.Value) continue;
                                        using (SqliteCommand update = connection.CreateCommand())
                                        {
                                            update.Transaction = transaction;
                                            update.CommandText = "UPDATE DatabaseDetails SET " + Quote(column) + "=$value WHERE ID_DBline=1";
                                            update.Parameters.AddWithValue("$value", row[column]);
                                            update.ExecuteNonQuery();
                                        }
                                    }
                                    break;
                                }
                                continue;
                            }
                            WriteRows(connection, table.TableName, table, false, transaction);
                            using (SqliteCommand count = connection.CreateCommand())
                            {
                                count.Transaction = transaction;
                                count.CommandText = "SELECT count(*) FROM " + Quote(table.TableName);
                                if (Convert.ToInt64(count.ExecuteScalar(), CultureInfo.InvariantCulture) != table.Rows.Count)
                                    throw new InvalidDataException("Migrated row count does not match the legacy export for " + table.TableName);
                            }
                        }
                        using (SqliteCommand foreign = connection.CreateCommand())
                        {
                            foreign.Transaction = transaction; foreign.CommandText = "PRAGMA foreign_key_check";
                            using (SqliteDataReader rows = foreign.ExecuteReader())
                                if (rows.Read()) throw new InvalidDataException("Legacy database contains broken references; import was rolled back.");
                        }
                        transaction.Commit();
                    }
                }
                finally { Execute(connection, "PRAGMA foreign_keys=ON"); }
            }
        }

        private static string Affinity(Type type)
        {
            if (type == typeof(byte[])) return "BLOB";
            if (type == typeof(bool) || type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)) return "INTEGER";
            // TEXT retains decimal precision and round-trip date values.
            return "TEXT";
        }

        internal static DataTableReader ReadSnapshot(SqliteConnection connection, string sql)
        {
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    DataTable table = new DataTable();
                    table.Load(reader);
                    return table.CreateDataReader();
                }
            }
        }

        internal static string Quote(string identifier) { return "\"" + identifier.Replace("\"", "\"\"") + "\""; }

        internal static void Execute(SqliteConnection connection, string sql)
        {
            if (connection.State != ConnectionState.Open) connection.Open();
            using (SqliteCommand command = connection.CreateCommand()) { command.CommandText = sql; command.ExecuteNonQuery(); }
        }

        internal static void WriteRows(SqliteConnection connection, string table, DataTable rows, bool ignoreDuplicates = false, SqliteTransaction transaction = null)
        {
            bool opened = connection.State != ConnectionState.Open;
            if (opened) connection.Open();
            SqliteTransaction owned = transaction == null ? connection.BeginTransaction() : null;
            try
            {
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction ?? owned;
                    string columns = string.Join(",", rows.Columns.Cast<DataColumn>().Select(c => Quote(c.ColumnName)));
                    string parameters = string.Join(",", rows.Columns.Cast<DataColumn>().Select((c, i) => "$p" + i));
                    command.CommandText = "INSERT INTO " + Quote(table) + " (" + columns + ") VALUES (" + parameters + ")" + (ignoreDuplicates ? " ON CONFLICT DO NOTHING" : "");
                    for (int i = 0; i < rows.Columns.Count; i++) command.Parameters.Add(new SqliteParameter("$p" + i, DBNull.Value));
                    foreach (DataRow row in rows.Rows)
                    {
                        for (int i = 0; i < rows.Columns.Count; i++) command.Parameters[i].Value = row[i];
                        command.ExecuteNonQuery();
                    }
                }
                owned?.Commit();
            }
            finally { owned?.Dispose(); if (opened) connection.Close(); }
        }
    }
}
