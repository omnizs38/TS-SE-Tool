param([Parameter(Mandatory = $true)][string] $SetupPath)
$ErrorActionPreference = 'Stop'
$setup = (Resolve-Path $SetupPath).Path
$target = Join-Path $env:RUNNER_TEMP "tsset-installer-O'Reilly-Алматы"
$seed = 'user-data-must-survive-' + [guid]::NewGuid().ToString('N')
function Invoke-Setup {
    $run = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/DIR="' + $target + '"'), ('/LOG="' + $env:RUNNER_TEMP + '\tsset-install.log"')) -PassThru
    if (-not $run.WaitForExit(120000)) { $run.Kill(); throw 'Installer timed out' }
    if ($run.ExitCode -ne 0) { throw "Installer failed: $($run.ExitCode)" }
}
Invoke-Setup
foreach ($required in @('TS SE Tool.exe', 'TS SE Tool.dll', 'coreclr.dll', 'e_sqlite3.dll', 'migration/LegacySqlCeExport.exe')) {
    if (-not (Test-Path (Join-Path $target $required))) { throw "Fresh install is missing $required" }
}
# Seed both existing packaged filenames and untracked user files: overwriting a
# shipped legacy database on upgrade is the regression this test must detect.
$protected = @((Join-Path $target 'config.cfg'))
foreach ($folder in @('dbs', 'gameref')) {
    $path = Join-Path $target $folder
    New-Item -ItemType Directory -Force -Path $path | Out-Null
    $first = Get-ChildItem $path -File -Recurse | Select-Object -First 1
    if ($first) { $protected += $first.FullName }
    $protected += Join-Path $path 'user-owned.sqlite'
}
foreach ($path in $protected) { [IO.File]::WriteAllText($path, $seed, [Text.UTF8Encoding]::new($false)) }
Invoke-Setup
foreach ($path in $protected) {
    if (-not (Test-Path $path) -or [IO.File]::ReadAllText($path) -ne $seed) { throw "Upgrade replaced user data: $path" }
}
$uninstall = Join-Path $target 'unins000.exe'
$run = Start-Process -FilePath $uninstall -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -PassThru
if (-not $run.WaitForExit(120000)) { $run.Kill(); throw 'Uninstaller timed out' }
if ($run.ExitCode -ne 0) { throw "Uninstaller failed: $($run.ExitCode)" }
if (Test-Path (Join-Path $target 'TS SE Tool.exe')) { throw 'Uninstall retained the application executable' }
foreach ($path in $protected) {
    if (-not (Test-Path $path) -or [IO.File]::ReadAllText($path) -ne $seed) { throw "Uninstall removed user data: $path" }
}
Write-Output 'PASS: fresh install, reinstall/upgrade data preservation, Unicode/apostrophe path, uninstall retaining user data.'
Remove-Item $target -Recurse -Force
