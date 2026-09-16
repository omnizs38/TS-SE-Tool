# TS SE Tool

A maintained Windows save editor for **Euro Truck Simulator 2** and **American Truck Simulator**.

> [!WARNING]
> Always back up a profile before writing a save. Game updates can introduce fields that older editor builds do not understand.

## Project status

This repository is the maintained source of TS SE Tool. Runtime links, update checks, CI and release artifacts point only to `omnizs38/TS-SE-Tool`. Required Apache-2.0 and third-party attribution is retained.

The save pipeline supports save-file versions **61–97**, preserves unmodelled fields and writes through an atomic temporary file. ETS2/ATS 1.60–1.61 saves use version 97. Newer formats remain unverified until tested with real profiles.

## Features

- Edit local, custom-folder and Steam profiles and saves
- Edit player level, skills, company money, cities, garages, trucks and trailers
- Generate/edit freight-market jobs
- Generate, clear, inspect, copy and paste cargo-market offer seeds by company or city
- Copy/paste truck positions and complete GPS routes
- Export/import versioned `.tsconvoy` packages and create position variants across saves
- Safe GitHub Releases update checks (no background download, execution or self-overwrite)
- Headless save round-trip diagnostic with `--selftest`

## Downloads

Each tagged release produces two Windows packages:

- `TS-SE-Tool-<version>-portable.zip` — extract and run
- `TS-SE-Tool-<version>-setup.exe` — per-user installer with optional shortcuts

Both are built on GitHub's current `windows-2025` hosted image with the latest Visual Studio/MSBuild image. GitHub does not offer a hosted Windows 11 desktop runner; Windows Server 2025 is the current supported hosted build environment, while the generated application manifest and packages target Windows 10/11 x64-compatible systems.

## Requirements

- Windows 10 or Windows 11, x64-compatible
- .NET Framework 4.8
- Visual Studio 2022/2025 build tools with the .NET desktop workload for local builds

## Build

```powershell
nuget restore "TS SE Tool.sln" -NonInteractive
msbuild "TS SE Tool.sln" /m /p:Configuration=Release /p:Platform="Any CPU"
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
