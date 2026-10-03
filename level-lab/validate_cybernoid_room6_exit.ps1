param(
    [Parameter(Mandatory = $true)] [string] $CandidateTap
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$snapshot = Join-Path $PSScriptRoot 'cybernoid2\cybernoid2-loaded.z80'
$baseTap = Join-Path $PSScriptRoot 'cybernoid2\cybernoid2-original-copy.tap'
$trace = Join-Path $PSScriptRoot 'trace_room_reads.py'
$result = Join-Path $env:TEMP 'cybernoid-room6-exit-check.json'

# This input seed was discovered against the unmodified game.  It begins in
# Room 06 and reaches the engine's next room (11), exercising the true ULA
# collision map and the game's room loader—not a visual doorway heuristic.
$env:PYTHONPATH = (Resolve-Path (Join-Path $root 'tools')).Path
python $trace $snapshot $result --operations 3000000 --random-controls 75000:3 `
    --tap-image $CandidateTap --tap-diff-base $baseTap *> $null

$run = Get-Content -Raw $result | ConvertFrom-Json
Remove-Item -LiteralPath $result
if ($run.final_room_index -ne 11) {
    throw "Room 06 exit validation failed: expected engine room 11, got $($run.final_room_index)."
}

Write-Host "PASS: Room 06 reached engine room 11."
