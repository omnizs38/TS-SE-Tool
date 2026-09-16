param(
    [Parameter(Mandatory = $true)]
    [string] $PackageVersion
)

$ErrorActionPreference = 'Stop'
$portable = Join-Path $PSScriptRoot '../artifacts/portable'
$sourceZip = Join-Path $env:RUNNER_TEMP 'upstream-runtime-assets.zip'
$sourceExtract = Join-Path $env:RUNNER_TEMP 'upstream-runtime-assets'
$sourceUrl = 'https://github.com/LIPtoH/TS-SE-Tool/releases/download/v0.3.11.0/TS.SE.Tool.0.3.11.0.zip'
# This intentional sentinel is replaced with the hash reported by the first controlled CI download.
$expectedHash = '0000000000000000000000000000000000000000000000000000000000000000'

Remove-Item $portable, $sourceExtract -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $portable | Out-Null

Invoke-WebRequest -Uri $sourceUrl -OutFile $sourceZip
$actualHash = (Get-FileHash $sourceZip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw "Pinned upstream runtime checksum must be reviewed. Expected $expectedHash; actual $actualHash"
}

Expand-Archive -Path $sourceZip -DestinationPath $sourceExtract -Force
$img = Get-ChildItem $sourceExtract -Directory -Filter img -Recurse | Select-Object -First 1
if (-not $img) {
    throw 'The attributed runtime archive does not contain the img directory.'
}
$assetRoot = $img.Parent.FullName

# Only data/resources are imported. Never copy old executables, DLLs, configs, logs or updater files.
foreach ($directory in @('img', 'lang', 'gameref', 'dbs')) {
    $source = Join-Path $assetRoot $directory
    if (Test-Path $source) {
        Copy-Item $source $portable -Recurse -Force
    }
}
foreach ($file in @('heavy_cargoes.csv', 'HowTo.pdf')) {
    $source = Join-Path $assetRoot $file
    if (Test-Path $source) {
        Copy-Item $source $portable -Force
    }
}

Copy-Item (Join-Path $PSScriptRoot '../TS SE Tool/bin/Release/*') $portable -Recurse -Force
foreach ($file in @('LICENSE', 'NOTICE', 'README.md', 'DEPENDENCIES.md')) {
    Copy-Item (Join-Path $PSScriptRoot "../$file") $portable -Force
}

Get-ChildItem $portable -Directory -Filter updater -Recurse | Remove-Item -Recurse -Force
Get-ChildItem $portable -File -Include *.pdb,*.log,*.xml -Recurse | Remove-Item -Force

$exe = Join-Path $portable 'TS SE Tool.exe'
if (-not (Test-Path $exe)) {
    throw "Expected executable was not produced: $exe"
}
$fileVersion = (Get-Item $exe).VersionInfo.FileVersion
if ($fileVersion -notlike '1.61.1*') {
    throw "Unexpected executable version: $fileVersion"
}
foreach ($directory in @('img', 'lang', 'libs')) {
    if (-not (Test-Path (Join-Path $portable $directory))) {
        throw "Portable package is missing $directory."
    }
}

$archive = Join-Path $PSScriptRoot "../artifacts/TS-SE-Tool-$PackageVersion-portable.zip"
Compress-Archive -Path "$portable/*" -DestinationPath $archive -Force
Write-Host "Created $archive"
