# Contributing

## Before changing save serialization

1. Back up every test profile.
2. Test at least one ETS2 and one ATS save when the affected block exists in both games.
3. Run a no-edit load → save → reload round trip.
4. Confirm unknown fields and blocks are preserved.
5. Never commit real profiles, credentials, logs containing personal paths, or generated binaries.

## Build

Use Visual Studio 2022 with the .NET desktop development workload, or run:

```powershell
nuget restore "TS SE Tool.sln" -NonInteractive
msbuild "TS SE Tool.sln" /m /p:Configuration=Release /p:Platform="Any CPU"
```

## Pull requests

Keep a pull request focused, explain compatibility risk, and list manual validation. CI must pass before merge. Dependency updates should remain on versions compatible with .NET Framework 4.8 and the Windows-only native components.

Follow `.editorconfig`; avoid drive-by formatting of generated WinForms designer and resource files.
