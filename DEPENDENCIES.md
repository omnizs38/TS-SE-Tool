# Dependency policy

## Main application

The entire WinForms application targets **.NET 10 LTS** through an SDK-style project and modern PackageReferences. Self-contained `win-x86` packages include the serviced .NET runtime; users do not install a separate .NET 10 runtime. Package lock files are committed, and CI restores production/importer dependencies in locked mode.

| Package | Version | Purpose |
| --- | ---: | --- |
| Microsoft.Data.Sqlite | 10.0.12 | SQLite storage; SQLitePCLRaw/native SQLite are transitive dependencies |
| System.Resources.Extensions | 10.0.12 | Existing attributed WinForms/icon resources |

System.Text.Json and GZipStream come from .NET 10. The main app no longer references SQL Server Compact, SqlCeBulkCopy, SharpZipLib, OpenPainter.ColorPicker.dll, System.Web.Extensions, or the old Framework support packages.

## Optional one-time legacy importer

`tools/LegacySqlCeExport` is a separate **net48 x86** console executable. It uses Microsoft.SqlServer.Compact **4.0.8876.1** to read old `.sdf` files in read-only mode and export typed XML. Microsoft.NETFramework.ReferenceAssemblies.net48 **1.0.3** is a private build dependency. The importer is required only for old SQL CE data; normal application/database operation is entirely .NET 10 + SQLite.

Do not represent this utility as a modern SQL CE driver. SQL CE has no supported .NET 10 provider. Keeping a narrow read-only compatibility executable is deliberate; it preserves user data without loading the unsupported provider into the modern GUI. Required legacy runtime/redistributable components must be available for this optional operation.

## Native decoder

The pinned, attributed `SII_Decrypt.dll` remains the existing audited x86 native binary. Its upstream is discontinued and has no newer verified release. The .NET 10 app explicitly resolves it from `libs/SII_Decrypt.dll`, validates decode return codes, and bounds decoded output. Do not replace it with an unverified fork. Do not retarget the GUI to x64 without a verified replacement or isolated x86 decoder host.

## Runtime assets and licenses

The `img`, `lang`, `gameref`, and optional `dbs` assets originate from attributed upstream `LIPtoH/TS-SE-Tool v0.3.11.0`; packaging verifies SHA-256 `0732cd4d861bd53b1570b90ecf928bc085e8b627c16db35edc9a0324d616b0da` and copies only allowlisted data. Upstream executable/DLL/updater/config files are not imported. Preserve LICENSE and NOTICE. Historical third-party notices remain for derived code; removed binary dependencies are not part of the new application's dependency graph.

Dependabot monitors current SDK-style NuGet projects and GitHub Actions.
