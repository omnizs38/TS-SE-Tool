# TS SET
Truck Simulator Save Editor Tool

## Description
Small tool for editing save files of Euro Truck Simulator 2 and American Truck Simulator.

## OS
Windows x64

### Dependence
.NET Framework 4.8

## Build
The project is built automatically on Windows via GitHub Actions.

* Every push and pull request to `master`/`main` triggers a Release build and uploads the `.exe` (with dependencies) as a workflow artifact.
* Pushing a version tag (e.g. `v0.3.1`) builds a Release and publishes a packaged `.zip` to GitHub Releases.

To build locally you need Visual Studio 2019+ (or MSBuild) with the .NET Framework 4.8 targeting pack:

```
nuget restore "TS SE Tool.sln"
msbuild "TS SE Tool.sln" /p:Configuration=Release
```

## You can:
* add Custom paths for save files.
* edit Local and Steam save files.
* edit Player level and skill.
* edit and share saved User Colors for truck and trailer.
* edit amount of Money on account.
* visit Cities and be able to grab cargo from discovered cities.
* buy and\or upgrade Garages.
* repair and\or refuel your Truck.
* Share truck paint job.
* repair Trailer.
* create custom jobs for Freight market.
* make basic edits to Cargo market.
* share Truck position.
* share GPS paths.
* share Multiple Truck positions as one Convoy Control pack.

## Short term goals:
* finish sharing functions for truck parts.
* add editing and share functions for trailers.
 

## Long term goals:
* add the ability to creat jobs for Cargo market (have couple ideas)
* get map data from game\game generated files.
* scan mods for data (trucks, cargo...)
