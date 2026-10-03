param(
    [Parameter(Mandatory = $true)] [string] $CandidateTap,
    [Parameter(Mandatory = $true)] [int] $StartRoom,
    [Parameter(Mandatory = $true)] [int] $ExpectedRoom,
    [int] $Seed = 3,
    [string] $Controls = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$snapshot = Join-Path $PSScriptRoot 'cybernoid2\cybernoid2-loaded.z80'
$baseTap = Join-Path $PSScriptRoot 'cybernoid2\cybernoid2-original-copy.tap'
$trace = Join-Path $PSScriptRoot 'trace_room_reads.py'
$probeMaker = Join-Path $PSScriptRoot 'make_scene_start_probe.py'
$probe = Join-Path $env:TEMP ("cybernoid-room-$StartRoom-probe.tap")
$result = Join-Path $env:TEMP ("cybernoid-room-$StartRoom-exit-check.json")

$env:PYTHONPATH = (Resolve-Path (Join-Path $root 'tools')).Path
python $probeMaker $CandidateTap $probe $StartRoom *> $null
if ($Controls) {
    python $trace $snapshot $result --operations 4000000 --controls-after "300000:$Controls" `
        --tap-image $probe --tap-diff-base $baseTap *> $null
} else {
    python $trace $snapshot $result --operations 3000000 --random-controls "75000:$Seed" `
        --tap-image $probe --tap-diff-base $baseTap *> $null
}

$run = Get-Content -Raw $result | ConvertFrom-Json
Remove-Item -LiteralPath $probe, $result
if ($run.final_room_index -ne $ExpectedRoom) {
    throw "Room $StartRoom exit validation failed: expected engine room $ExpectedRoom, got $($run.final_room_index)."
}

Write-Host "PASS: Room $StartRoom reached engine room $ExpectedRoom."
