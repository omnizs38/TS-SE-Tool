# .NET 10 / SQLite migration status

## Implemented

- Complete WinForms SDK-style `net10.0-windows` application, PackageReferences and locked production/importer restores.
- Self-contained `win-x86` publish for the retained audited x86 SII decoder; no separate modern .NET runtime needed.
- SQLite replaces every SQL CE GUI connection, schema, bulk insert, reference cache, and route query. Native SQL CE is not loaded into the main app.
- Parameterized inserts, transactions, foreign keys, globally unique SQLite indexes, exact decimal cache values, and real route upserts. Existing city/company/cargo caches can now receive new entries without the old broken list-difference logic or UNION-size limits.
- Original `.sdf` files remain intact. The isolated read-only net48 importer exports typed rows; staged imports check row counts and foreign keys before publication. Unknown/custom table data is preserved; application metadata is normalized.
- System.Text.Json replaces System.Web, GZipStream replaces SharpZipLib, and the maintained Windows color dialog replaces the old external OpenPainter binary.
- Modern cancellation-aware streamed update downloads, checksum validation before atomic staging, Unicode-safe exit installation, and a second on-disk hash check before execution.
- JSON preferences stored atomically in local application data, with bounded/secure legacy XML import. Automatic installation is opt-in when legacy preferences cannot be recovered.
- .NET 10 regression harnesses, SQLite/typed-migration tests, complete-package WinForms/native smoke test, and real synthetic SQL CE import on Windows CI.

## Deliberately retained compatibility components

The main application is not net48. A narrow **optional net48 x86 importer** remains solely because SQL CE has no supported .NET 10 provider. It is not required for normal SQLite operation. The native x86 SII decoder remains attributed and pinned rather than replaced with an unverified binary. These exceptions must be stated honestly in release notes.

## Release gates

- Windows 10/11 game-save no-edit and edited round trips for ETS2 and ATS; save versions 61–97 and unknown blocks/fields.
- High-DPI/narrow-window interaction, native color selection/reset, and real profile/cache data migration including older schema versions.
- Installer upgrade/uninstall, runtime-less modern startup, failed-upgrade recovery, optional importer prerequisites, update-after-exit, and OS servicing/ESU compatibility.
- Multi-file profile/info/game consistency is still not transactional; keep backups and do not claim otherwise.

No new version/tag/release is implied by this development migration. Build validation does not replace the manual game/installer gates.

## Sources

[Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) identifies .NET 10 as the active LTS line. Use serviced runtime patches and supported Windows builds. The native/legacy component provenance is documented in `DEPENDENCIES.md` and `NOTICE`.
