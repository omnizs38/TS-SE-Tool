# TS SE Tool 1.63.0

## Highlights

- Migrated the complete WinForms application to self-contained .NET 10 LTS, with locked modern package restores. No separate .NET 10 installation is required.
- Replaced the GUI's SQL Server Compact database layer with SQLite: parameterized bulk writes, transactional imports, foreign-key validation, exact decimal values, and route upserts.
- Added isolated, read-only migration of existing `.sdf` databases. The originals remain unchanged; typed rows and custom tables are checked before publishing the new database.
- Replaced legacy web/JSON, compression, settings and external color-picker dependencies with System.Text.Json, BCL gzip, atomic JSON preferences and the Windows color dialog.
- Hardened save writes: fingerprint loaded files, serialize/stage every edit first, complete matching backups, replace files atomically, and roll back already-published files after ordinary I/O failures. Prevent application exit during background save/cache operations.
- Fixed array edits resurrecting removed entries during unknown-field preservation. Fixed URLs, folders and help documents failing to open after the modern .NET migration.
- Preserve existing database/cache/config files during installation and uninstall. Automatic update downloads remain checksum-verified; missing or unreadable old preferences do not silently enable automatic installation.
- Made diagnostic self-tests return failure for lost/duplicate blocks or changed profile/info content, support LF files, and refuse diagnostic output inside the source save folder.

## Requirements and upgrade

- Windows 11, or serviced Windows 10 22H2 (build 19045+) with applicable ESU/security updates, on an x64-compatible system. Windows 7/8 are no longer supported.
- The packaged application remains **win-x86** because the attributed, pinned SII decoder is x86. It has not been replaced with an unverified binary.
- The **optional legacy `.sdf` importer** still requires .NET Framework 4.8 and runs in a separate process. Normal SQLite operation does not require the importer or SQL CE.
- **Back up your entire game profile and the tool's `dbs`, `gameref` and `config.cfg` before upgrading. Close the game before loading or editing saves.** Install over the previous setup, or extract the portable archive to a new directory and copy your existing data there rather than overwriting it with packaged defaults.
- Recover existing databases on first use. If migration fails, the original `.sdf` is preserved; consult `errorlog.log` and do not delete it.

## Automated validation

The release workflow must pass before publication:

- Complete Windows application/importer build and self-contained portable/setup packaging.
- Shared and Windows updater/settings regressions, SQLite schema/import/precision checks, and save-batch failure injection, rollback and concurrent-change detection.
- Complete-package WinForms/resource/native-decoder smoke test and real SQL CE-to-SQLite import using a **synthetic** fixture, verifying the original `.sdf` checksum remains unchanged.
- Production serialization tests with **synthetic** no-edit and edited fixtures for save-file versions **61–97**, unknown scalar/array/block preservation, extended profile data, and array shrinking.
- Fresh installation, reinstall/upgrade protecting existing shipped and user-owned database/cache/config files, Unicode/apostrophe installation paths, and uninstall retaining user data.
- SHA-256 checksums published with both portable and setup assets.

## Known validation limits

No real ETS2/ATS profile or game installation was supplied for this release. In-game loading of edited saves, historical installer upgrades, Windows 10/11 high-DPI interaction, and the full update-after-exit lifecycle have **not** been manually validated. CI runs on Windows Server 2025 and is not a substitute for those checks.

Per-file atomic replacement and rollback are **not** a power-loss-safe cross-file transaction. If recovery reports incomplete rollback, restore the matching `profile_backup.sii`, `info_backup.sii` and `game_backup.sii` from the same attempt before continuing. Compatibility with save formats newer than 97 remains unverified.
