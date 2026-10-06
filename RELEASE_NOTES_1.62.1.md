# TS SE Tool 1.62.1

## Highlights

- Improved Cargo Market and Convoy layouts at narrow window widths and high display scaling.
- Reduced UI churn by debouncing resize and DPI relayouts and removing duplicate application-wide idle polling.
- Fixed event-driven automatic selection of the only available truck or trailer.
- Fixed GDI resource leaks by disposing replaced company-logo images.
- Removed the legacy Cargo Market layout pass that conflicted with the maintained UI.
- Updated ETS2/ATS 1.61 compatibility documentation and included COMPATIBILITY.md in portable packages.

## Validation and safety

The underlying changes passed the repository's Windows build and packaging CI. The release workflow rebuilds the portable archive and per-user Windows installer and publishes SHA-256 checksums before publishing this release.

Save-file versions 61–97 remain supported. Compatibility with future save formats is not guaranteed. Always back up a profile before editing a save.
