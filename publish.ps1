<#
.SYNOPSIS
  Test and publish Speccy Studio as a self-contained Windows build.

.DESCRIPTION
  1. Runs the level-lab smoke tests (Exolon engine, Rex lab, Myth lab) unless -SkipTests is given.
  2. Publishes Release / win-x64 / self-contained into -Output (default: "..\Speccy Studio BUILD", next to this folder).
  3. With -Deploy, copies that build over the live app folder ("..\Speccy Studio"). The live folder's Projects folder
     (your vault) is never touched, nothing is deleted, and it refuses to run while SpeccyStudio.exe is open.

.EXAMPLE
  .\publish.ps1                 # test + publish to "..\Speccy Studio BUILD"
  .\publish.ps1 -Deploy         # ...then update the live "..\Speccy Studio" folder
  .\publish.ps1 -SkipTests      # publish only
#>
[CmdletBinding()]
param(
    [string]$Output,
    [string]$LiveFolder,
    [switch]$Deploy,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$parent = Split-Path -Parent $root
if (-not $Output) { $Output = Join-Path $parent 'Speccy Studio BUILD' }
if (-not $LiveFolder) { $LiveFolder = Join-Path $parent 'Speccy Studio' }

function Step($text) { Write-Host "`n=== $text" -ForegroundColor Cyan }

if (-not $SkipTests) {
    Step 'Building and running the level-lab smoke tests'
    foreach ($test in 'exolon-engine', 'rex-lab', 'myth-lab') {
        & dotnet run --project (Join-Path $root 'SpeccyStudio.SmokeTests') -c Release -- $test
        if ($LASTEXITCODE -ne 0) { throw "Smoke test '$test' failed - not publishing." }
    }
}

Step "Publishing to $Output"
if (Test-Path -LiteralPath $Output) {
    $resolved = (Resolve-Path -LiteralPath $Output).Path
    if ($resolved -eq $root -or $resolved -eq $LiveFolder -or $resolved.Length -lt 12) { throw "Refusing to clear '$resolved'." }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
& dotnet publish (Join-Path $root 'SpeccyStudio\SpeccyStudio.csproj') -c Release -r win-x64 --self-contained true -o $Output
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$exe = Join-Path $Output 'SpeccyStudio.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Publish finished but $exe is missing." }
$size = '{0:N0}' -f ((Get-ChildItem -LiteralPath $Output -Recurse -File | Measure-Object Length -Sum).Sum / 1MB)
Write-Host "Built $exe  ($size MB total)" -ForegroundColor Green

if ($Deploy) {
    Step "Deploying to $LiveFolder"
    if (-not (Test-Path -LiteralPath $LiveFolder)) { throw "Live folder '$LiveFolder' does not exist." }
    if (Get-Process -Name 'SpeccyStudio' -ErrorAction SilentlyContinue) { throw 'SpeccyStudio.exe is running - close it and run again.' }
    & robocopy $Output $LiveFolder /E /XD Projects /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed (exit code $LASTEXITCODE)." }
    Write-Host "Updated $LiveFolder (Projects folder left alone)." -ForegroundColor Green
}
else {
    Write-Host "`nRun again with -Deploy to update '$LiveFolder', or run the exe from '$Output'." -ForegroundColor Yellow
}
