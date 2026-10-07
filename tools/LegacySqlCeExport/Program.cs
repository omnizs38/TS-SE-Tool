using System;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Xml;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--create-fixture") { CreateFixture(args[1]); return 0; }
            if (args.Length != 2) { Console.Error.WriteLine("Usage: LegacySqlCeExport <source.sdf> <new-output.xml>"); return 2; }
            string source = Path.GetFullPath(args[0]);
            string output = Path.GetFullPath(args[1]);
            if (!File.Exists(source) || File.Exists(output) || source == output) throw new IOException("Invalid export paths.");
            using (SqlCeConnection connection = new SqlCeConnection(new SqlCeConnectionStringBuilder { DataSource = source, FileMode = "Read Only", TempFilePath = Path.GetTempPath() }.ToString()))
            using (DataSet data = new DataSet("LegacyDatabase"))
            {
                connection.Open();
                DataTable tables = connection.GetSchema("Tables");
                foreach (DataRow info in tables.Rows)
                {
                    if (Convert.ToString(info["TABLE_TYPE"]) != "TABLE") continue;
                    string name = Convert.ToString(info["TABLE_NAME"]);
                    using (SqlCeCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM [" + name.Replace("]", "]]" ) + "]";
                        using (SqlCeDataReader reader = command.ExecuteReader())
                        {
                            DataTable table = new DataTable(name);
                            table.Load(reader);
                            data.Tables.Add(table);
                        }
                    }
                }
                using (FileStream file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (XmlWriter writer = XmlWriter.Create(file, new XmlWriterSettings { Indent = false }))
                    data.WriteXml(writer, XmlWriteMode.WriteSchema);
            }
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void CreateFixture(string path)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path)) throw new IOException("Fixture already exists; refusing overwrite.");
        using (SqlCeEngine engine = new SqlCeEngine(new SqlCeConnectionStringBuilder { DataSource = path }.ToString())) engine.CreateDatabase();
        using (SqlCeConnection connection = new SqlCeConnection(new SqlCeConnectionStringBuilder { DataSource = path }.ToString()))
        {
            connection.Open();
            foreach (string sql in new[] {
                "CREATE TABLE DatabaseDetails (ID_DBline INT PRIMARY KEY, GameName NVARCHAR(8), SaveVersion INT, ProfileName NVARCHAR(128), DBVersion NVARCHAR(16))",
                "INSERT INTO DatabaseDetails VALUES (1,'ETS2',97,'SyntheticProfile','0.2.6')",
                "CREATE TABLE CitysTable (ID_city INT PRIMARY KEY, CityName NVARCHAR(32))",
                "CREATE TABLE LegacyCustom (ID INT, Amount NUMERIC(18,6), Enabled BIT, Payload IMAGE, Optional NVARCHAR(32))" })
            using (SqlCeCommand command = new SqlCeCommand(sql, connection)) command.ExecuteNonQuery();
            using (SqlCeCommand command = new SqlCeCommand("INSERT INTO CitysTable VALUES (7,@name)", connection))
            { command.Parameters.AddWithValue("@name", "O'Reilly–Алматы"); command.ExecuteNonQuery(); }
            using (SqlCeCommand command = new SqlCeCommand("INSERT INTO LegacyCustom VALUES (3,123.456789,1,@bytes,NULL)", connection))
            { command.Parameters.AddWithValue("@bytes", new byte[] { 0, 1, 255 }); command.ExecuteNonQuery(); }
        }
    }
}
