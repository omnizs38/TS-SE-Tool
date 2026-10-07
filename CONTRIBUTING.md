# Contributing

## Save compatibility

1. Back up test profiles and legacy databases.
2. Test at least one ETS2 and ATS save when the affected block is shared.
3. Run a no-edit load → save → reload round trip and an edited round trip.
4. Verify unknown fields/blocks survive serialization.
5. Never commit personal profiles, credentials, personal-path logs, or generated binaries.

## Build and test

Use the .NET 10 SDK. WinForms/native smoke tests run on Windows; the optional legacy importer runs on .NET Framework 4.8. Modern IDE support must match the chosen SDK.

```powershell
dotnet publish 'TS SE Tool/TS SE Tool.csproj' -c Release -r win-x86 --self-contained true -o artifacts/publish -p:RestoreLockedMode=true
dotnet build tools/LegacySqlCeExport -c Release -p:RestoreLockedMode=true
dotnet run --project tests/TS-SE-Tool.RegressionTests -c Release
dotnet run --project tests/TS-SE-Tool.ModernRegressionTests -c Release
dotnet run --project tests/TS-SE-Tool.StorageTests -c Release
```

Update committed production/importer lock files intentionally when dependencies change. The isolated compatibility helper is the only net48 executable. The main app must not regain direct SQL CE or legacy binary UI dependencies.

## Pull requests

Keep changes focused, document migration/compatibility risks, and list validation. CI must pass before merge. Real game-save, DPI, installer upgrade, and recovery checks are required before release. Follow `.editorconfig`; avoid drive-by formatting of generated WinForms files. Atomic file replacement is per-file, not a multi-file transaction.
