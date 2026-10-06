# Stability and modernization workstream

## Goal

Modernize the maintained TS SE Tool without losing save data, introducing unverified native binaries, or silently changing supported Windows versions. This document describes work in progress, not a released feature set.

## First implementation slice

- Replace delete-then-move save writes with a shared sibling-file writer that flushes data and uses `File.Replace` for existing files. Failed replacement never falls back to deleting the original.
- Use the same writer for settings so a serialization or filesystem failure preserves the previous configuration.
- Validate settings field by field: malformed timestamps, non-finite multipliers, and values outside the UI's ranges must not abort loading or overwrite an existing config.
- Prevent the pickup-time control from overflowing its day limit at 384 hours.
- Reject overflowing or malformed release versions, exact-match checksum filenames, reject conflicting hashes, and observe cancellation before staging an executable update.
- Build and run linked-production-code regression tests before packaging in CI.

Atomic replacement applies to one file at a time. A profile/info/game save set is not yet a multi-file transaction; existing backups remain necessary. Installer execution and actual game-save round trips still require Windows/manual validation.

## Dependency audit

All eight currently pinned NuGet packages match the latest stable versions in their NuGet flat-container indexes as checked on 2026-10-06. There is no package bump to apply in this slice. See `DEPENDENCIES.md` for the pins and native components. Do not replace discontinued components with unverified forks merely to obtain a newer version number.

## Next slices and gates

1. **Save correctness:** add anonymized/synthetic ETS2 and ATS round-trip fixtures for supported save versions 61–97, unknown fields/blocks, and edited profiles. Add fault-injection coverage for multi-file writes before designing rollback.
2. **Lifecycle and responsiveness:** audit cancellation on form disposal, resource ownership, and database reader/command disposal; replace remaining blocking UI work only where it is measured or reproducible.
3. **Build modernization:** evaluate SDK-style projects and `PackageReference` while retaining net48 as a transitional baseline. Verify WinForms resources/designers, binding redirects, and SQL CE native packaging before switching.
4. **Storage migration:** map the current `.sdf` data and SQL CE bulk-copy call sites; prototype a supported replacement such as SQLite. Specify import/rebuild, rollback, and performance requirements before removing SQL CE.
5. **Modern .NET:** move WinForms to a supported .NET LTS only after the Windows support baseline and storage/native-decoder strategy are approved. Keep a separate legacy path if older systems are still required.
6. **Release:** verify Windows builds, portable/setup packaging, checksum generation, real ETS2/ATS no-edit and edited round trips, and DPI layouts. Do not merge or publish until required checks pass.

## Run regression tests

On Windows with Visual Studio 2022 Build Tools and the .NET Framework 4.8 targeting pack:

```powershell
msbuild 'tests/TS-SE-Tool.RegressionTests/TS-SE-Tool.RegressionTests.csproj' /m /p:Configuration=Release
& './tests/TS-SE-Tool.RegressionTests/bin/Release/TS-SE-Tool.RegressionTests.exe'
```

The harness links the production implementations directly and supplies only UI/logging adapters. Tests use temporary synthetic files and do not need personal profiles, live GitHub downloads, or installer execution.

## Decisions still needed

- Keep the current .NET Framework 4.8/Windows compatibility baseline, or approve a supported modern Windows + .NET LTS baseline?
- Should existing `.sdf` databases be imported, regenerated from source data, or retained by a separate migration utility? Assess which contain user-created data before deciding.
