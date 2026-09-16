# Dependency policy

The application targets .NET Framework 4.8 because SQL Server Compact and the native SII decoder remain Windows-only dependencies. Packages are restored from NuGet; generated DLLs are not committed.

## NuGet packages

| Package | Version | Notes |
| --- | ---: | --- |
| ErikEJ.SqlCeBulkCopy | 2.1.6.15 | Latest published compatible release |
| Microsoft.SqlServer.Compact | 4.0.8876.1 | Latest published SQL CE 4 package |
| SharpZipLib | 1.4.2 | Latest published stable release |
| System.Buffers | 4.6.1 | Latest published stable release |
| System.Memory | 4.6.3 | Latest published stable release |
| System.Numerics.Vectors | 4.6.1 | Latest published stable release |
| System.Runtime.CompilerServices.Unsafe | 6.1.2 | Latest stable line compatible with .NET Framework 4.8 |
| System.Threading.Tasks.Extensions | 4.6.3 | Latest published stable release |

Dependabot monitors NuGet and GitHub Actions updates.

## Bundled native/legacy components

- `SII_Decrypt.dll` is required for SCS encrypted/binary saves. The upstream project is discontinued and does not publish a newer verified binary release, so this binary is retained rather than replaced with an unverified fork.
- `OpenPainter.ColorPicker.dll` supplies controls used by the in-tree attributed color-picker implementation. No newer compatible package is published.
- SQL Server Compact native x86/amd64 binaries come from the pinned NuGet package during each build.

## Runtime data provenance

The `img`, `lang`, `gameref` and optional `dbs` data were distributed under the project's Apache-2.0 upstream release `LIPtoH/TS-SE-Tool v0.3.11.0`. Packaging downloads that immutable archive with a pinned SHA-256 and copies only those allowlisted data directories. Old executables, DLLs, configuration, logs and the legacy updater are never copied. Runtime update checks and all user-facing links point only to `omnizs38/TS-SE-Tool`.
