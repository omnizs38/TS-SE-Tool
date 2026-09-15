# TS SE Tool

A maintained Windows save editor for **Euro Truck Simulator 2** and **American Truck Simulator**.

> [!WARNING]
> Always back up a profile before writing a save. Game updates can introduce fields that older editor builds do not understand.

## Project status

This repository is the maintained source of the project. Runtime links, update instructions, CI, and release artifacts point only to `omnizs38/TS-SE-Tool`.

The current save pipeline supports save-file versions **61–97** and preserves unmodelled fields during a load → save round trip. ETS2/ATS 1.60–1.61 saves use version 97. Newer game versions must be treated as unverified until tested with real profiles.

The legacy in-app updater has been removed. Install updates manually from this repository's **GitHub Releases** page; builds never download and overwrite application files in the background.

## Features

- Edit local and Steam profiles and saves
- Edit player level, skills, money, cities, and garages
- Repair/refuel trucks and trailers
- Create freight-market jobs and make basic cargo-market edits
- Import/export colors, paint jobs, positions, and GPS routes
- Run a headless save round-trip diagnostic with `--selftest`

## Requirements

- Windows 10 or Windows 11, x64
- .NET Framework 4.8 or newer 4.x runtime
- Visual Studio 2022 with the **.NET desktop development** workload for local builds

The application remains on .NET Framework because SQL Server Compact and the native save decoder are Windows-only legacy dependencies. Moving to modern .NET requires replacing those components first; changing the target alone would create a non-working build.

## Build

```powershell
nuget restore "TS SE Tool.sln" -NonInteractive
msbuild "TS SE Tool.sln" /m /p:Configuration=Release /p:Platform="Any CPU"
```

The executable and runtime files are written to `TS SE Tool/bin/Release`.

## Diagnostic round trip

```powershell
& ".\TS SE Tool.exe" --selftest "C:\path\to\profile\save\slot" "C:\temp\tsset-report"
```

The diagnostic does not write into the source save directory. Do not attach personal save data to public issues; share only a minimized, sanitized reproduction.

## Releases and support

- Releases: <https://github.com/omnizs38/TS-SE-Tool/releases>
- Bugs and feature requests: <https://github.com/omnizs38/TS-SE-Tool/issues>
- Security reports: see [SECURITY.md](SECURITY.md)
- Contributions: see [CONTRIBUTING.md](CONTRIBUTING.md)

## License and attribution

Licensed under Apache-2.0. Required upstream and third-party attribution is retained in [LICENSE](LICENSE) and [NOTICE](NOTICE); stale runtime branding and update endpoints are not.
