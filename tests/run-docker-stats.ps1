param(
    [int]$DurationSeconds = 60,
    [int]$IntervalSeconds = 5,
    [string]$OutputFile = "",
    [string]$Label = "manual-docker-stats"
)

$ErrorActionPreference = "Stop"

$resultsDir = Join-Path $PSScriptRoot "results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputFile = Join-Path $resultsDir "docker-stats-$timestamp.txt"
}

Write-Host "Collecting Docker stats for $DurationSeconds seconds..."
Write-Host "Output: $OutputFile"

& (Join-Path $PSScriptRoot "docker-stats/collect-docker-stats.ps1") `
    -DurationSeconds $DurationSeconds `
    -IntervalSeconds $IntervalSeconds `
    -OutputFile $OutputFile `
    -Label $Label
