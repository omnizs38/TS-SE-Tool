# Stability and modernization workstream

## Goal

Modernize the maintained TS SE Tool without losing save data, introducing unverified native binaries, or silently changing supported Windows versions. This document describes work in progress, not a released feature set.

## Approved target

The selected target is **.NET 10 LTS with WinForms on Windows 10/11**, not a permanent net48-only application. .NET 10 is the active LTS release; use serviced runtime patches and verify the exact Windows builds against Microsoft's supported-OS matrix before the modern installer is published.

This branch does not yet retarget the shipping WinForms application. SQL CE storage and the native decoder need a verified migration strategy first. The current net48 build stays as a regression baseline during the transition.

## First implementation slice

- Replace delete-then-move save writes with a shared sibling-file writer that flushes data and uses `File.Replace` for existing files. Failed replacement never falls back to deleting the original.
- Use the same writer for settings so a serialization or filesystem failure preserves the previous configuration.
- Validate settings field by field: malformed timestamps, non-finite multipliers, and values outside the UI's ranges must not abort loading or overwrite an existing config.
- Prevent the pickup-time control from overflowing its day limit at 384 hours.
- Reject overflowing or malformed release versions, exact-match checksum filenames, reject conflicting hashes, and observe cancellation before staging an executable update.
- Build and run linked-production-code regression tests before packaging in CI.
- Extract runtime-neutral release validation and run the same file/settings/version/checksum regressions on both .NET Framework 4.8 and .NET 10.
- Update cache, MSBuild, and NuGet setup actions to their verified Node.js 24 releases; add the current .NET setup action.

Atomic replacement applies to one file at a time. A profile/info/game save set is not yet a multi-file transaction; existing backups remain necessary. Installer execution and actual game-save round trips still require Windows/manual validation.

## Dependency audit

All eight currently pinned NuGet packages match the latest stable versions in their NuGet flat-container indexes as checked on 2026-10-06. There is no package bump to apply in this slice. See `DEPENDENCIES.md` for the pins and native components. Do not replace discontinued components with unverified forks merely to obtain a newer version number.

## Next slices and gates

1. **Save correctness:** add anonymized/synthetic ETS2 and ATS round-trip fixtures for supported save versions 61–97, unknown fields/blocks, and edited profiles. Add fault-injection coverage for multi-file writes before designing rollback.
2. **Lifecycle and responsiveness:** audit cancellation on form disposal, resource ownership, and database reader/command disposal; replace remaining blocking UI work only where it is measured or reproducible.
3. **Build modernization:** evaluate SDK-style projects and `PackageReference` while retaining net48 as a transitional baseline. Verify WinForms resources/designers, binding redirects, and SQL CE native packaging before switching.
4. **Storage migration:** map the current `.sdf` data and SQL CE bulk-copy call sites; prototype a supported replacement such as SQLite. Specify import/rebuild, rollback, and performance requirements before removing SQL CE.
5. **Modern .NET:** migrate WinForms to .NET 10 LTS for Windows 10/11 after validating storage and native-decoder replacements. Decide whether x86 decoding needs isolation in a separate process or can be safely retained in-process. Validate the modern installer, runtime distribution, and upgrade path from existing net48 installs.
6. **Release:** verify Windows builds, portable/setup packaging, checksum generation, real ETS2/ATS no-edit and edited round trips, and DPI layouts. Do not merge or publish until required checks pass.

## Run regression tests

On Windows with Visual Studio 2022 Build Tools and the .NET Framework 4.8 targeting pack:

```powershell
msbuild 'tests/TS-SE-Tool.RegressionTests/TS-SE-Tool.RegressionTests.csproj' /m /p:Configuration=Release
& './tests/TS-SE-Tool.RegressionTests/bin/Release/TS-SE-Tool.RegressionTests.exe'
# With the .NET 10 SDK installed:
dotnet run --project 'tests/TS-SE-Tool.ModernRegressionTests/TS-SE-Tool.ModernRegressionTests.csproj' --configuration Release
```

The harness links the production implementations directly and supplies only UI/logging adapters. Tests use temporary synthetic files and do not need personal profiles, live GitHub downloads, or installer execution.

## Decisions still needed

- Verify which Windows 10/11 builds and servicing channels are supported by the chosen .NET 10 release. Windows 10 servicing/ESU requirements must not be confused with application compatibility.
- Should existing `.sdf` databases be imported, regenerated from source data, or retained by a separate migration utility? Assess which contain user-created data before deciding.

## Platform source

[Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) identifies .NET 10 as the active LTS line. The cross-runtime tests are a migration gate, not evidence that SQL CE, WinForms designers, the decoder, or the full shipping application already work on modern .NET.
