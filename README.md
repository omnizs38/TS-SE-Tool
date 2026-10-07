# TS SE Tool

A maintained Windows save editor for **Euro Truck Simulator 2** and **American Truck Simulator**.

> [!WARNING]
> Always back up a profile before writing a save. Game updates can introduce fields that older editor builds do not understand.

## Project status

This repository is the maintained source of TS SE Tool. Runtime links, update checks, CI and release artifacts point only to `omnizs38/TS-SE-Tool`. Required Apache-2.0 and third-party attribution is retained.

The save pipeline supports save-file versions **61–97**, preserves unmodelled fields and writes through an atomic temporary file. The current ETS2 and ATS 1.61 releases use save-file version 97. Newer formats remain unverified until tested with real profiles; the editor shows a warning instead of silently claiming compatibility.

See [COMPATIBILITY.md](COMPATIBILITY.md) for the verified game/save matrix and validation policy.

## Features

- Edit local, custom-folder and Steam profiles and saves
- Edit player level, skills, company money, cities, garages, trucks and trailers
- Generate/edit freight-market jobs
- Generate, clear, inspect, copy and paste cargo-market offer seeds by company or city
- Copy/paste truck positions and complete GPS routes
- Export/import versioned `.tsconvoy` packages and create position variants across saves
- Secure background update downloads with SHA-256 verification and installation after application exit
- Headless save round-trip diagnostic with `--selftest`

## Downloads

Each tagged release produces two Windows packages:

- `TS-SE-Tool-<version>-portable.zip` — extract and run
- `TS-SE-Tool-<version>-setup.exe` — per-user installer with optional shortcuts

Both are built on GitHub's current `windows-2025` hosted image using the .NET 10 SDK. GitHub does not offer a hosted Windows 11 desktop runner; Windows Server 2025 is the current supported hosted build environment, while the generated application manifest and packages target Windows 10/11 x64-compatible systems.

## Requirements

- Windows 10 or Windows 11, x64-compatible
- No separate .NET installation for the self-contained .NET 10 application
- The optional one-time `.sdf` importer requires .NET Framework 4.8; normal operation and new SQLite databases do not
- .NET 10 SDK for local builds (Windows is required to run WinForms/native tests)

## Build

```powershell
dotnet publish "TS SE Tool/TS SE Tool.csproj" -c Release -r win-x86 --self-contained true -o artifacts/publish -p:RestoreLockedMode=true
dotnet build "tools/LegacySqlCeExport/LegacySqlCeExport.csproj" -c Release -p:RestoreLockedMode=true
dotnet run --project tests/TS-SE-Tool.RegressionTests -c Release
dotnet run --project tests/TS-SE-Tool.StorageTests -c Release
```

See [DEPENDENCIES.md](DEPENDENCIES.md) for package versions and native-component provenance.

## Diagnostic round trip

```powershell
& ".\TS SE Tool.exe" --selftest "C:\path\to\profile\save\slot" "C:\temp\tsset-report"
```

The diagnostic does not write into the source save directory. Never attach personal save data to public issues; share only a minimized, sanitized reproduction.

## Releases and support

- Releases: <https://github.com/omnizs38/TS-SE-Tool/releases>
- Bugs and feature requests: <https://github.com/omnizs38/TS-SE-Tool/issues>
- Security reports: [SECURITY.md](SECURITY.md)
- Contributions: [CONTRIBUTING.md](CONTRIBUTING.md)

## License and attribution

Licensed under Apache-2.0. Required upstream and third-party attribution is retained in [LICENSE](LICENSE) and [NOTICE](NOTICE); stale runtime branding, update endpoints and executable updater code are not.

## .NET 10 / SQLite migration

The development application uses an SDK-style `net10.0-windows` project, modern PackageReferences, System.Text.Json, BCL GZipStream, and Microsoft.Data.Sqlite. It is self-contained **win-x86** because the audited SII decoder is x86; it runs on x64-compatible Windows 10/11. The installer requires Windows 10 build 17763 or newer. OS servicing/support requirements still apply.

On first use, a sibling legacy `.sdf` is read by the isolated `migration/LegacySqlCeExport.exe` compatibility utility, then imported into a staged `.sqlite`. The original `.sdf` is never deleted or overwritten. Row counts and foreign keys are checked before the new database is published. Unknown/custom tables are copied, while application metadata is normalized to the current schema. If the importer, runtime, or source data is unavailable/invalid, migration fails without publishing an empty replacement. Back up the `dbs`, `gameref/cache`, `config.cfg`, and game profiles before upgrading.

Existing startup/update preferences are imported from the known legacy company/application user.config folders when no modern user.config exists. Original preference files remain untouched. If an installation used a different historical company/application identity, verify those three preferences manually.

The maintained Windows color dialog replaces the old external OpenPainter binary, retaining color selection and transparent/reset behavior. Legacy SQL CE native files are confined to the optional importer, not the application's runtime dependencies.

CI builds the complete application, runs Windows updater/settings and shared/SQLite regressions, and smoke-tests the actual packaged WinForms resources, native decoder, and a synthetic SQL CE-to-SQLite migration. Real ETS2/ATS saves, high-DPI interaction, and installer upgrade/rollback remain manual release gates; a green build alone is not a claim that all game scenarios were tested.
