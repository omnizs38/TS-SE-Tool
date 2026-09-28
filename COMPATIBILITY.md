# Game and save compatibility

Last reviewed: 2026-09-28

| Game | Current verified game line | Save-file version | Status |
| --- | --- | ---: | --- |
| Euro Truck Simulator 2 | 1.61.x | 97 | Supported |
| American Truck Simulator | 1.61.x | 97 | Supported |

SCS Software released ATS 1.61 on 2026-09-15 and ETS2 1.61 on 2026-09-17:

- <https://blog.scssoft.com/2026/09/american-truck-simulator-161-update-release.html>
- <https://blog.scssoft.com/2026/09/euro-truck-simulator-2-161-update.html>

## Compatibility policy

- Save-file versions 61 through 97 are accepted.
- Version 97 has been checked with load, write and reload diagnostics.
- Unmodelled blocks and attributes are preserved when a save is written.
- A save newer than version 97 requires explicit confirmation and must be tested on a backup.
- Public beta and future game formats are not marked as supported until a real save completes the round-trip self-test.

Run the diagnostic without modifying the original save:

```powershell
& ".\TS SE Tool.exe" --selftest "C:\path\to\profile\save\slot" "C:\temp\tsset-report"
```