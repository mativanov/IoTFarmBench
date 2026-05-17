param(
    [int[]]$Vus = @(10, 100, 500),
    [string]$Duration = "",
    [string]$NetworkName = "iotfarmbench_default",
    [int]$StatsIntervalSeconds = 5,
    [switch]$SkipGrpc
)

$ErrorActionPreference = "Continue"

$resultsDir = Join-Path $PSScriptRoot "results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

if ([string]::IsNullOrWhiteSpace($Duration)) {
    $Duration = if ([string]::IsNullOrWhiteSpace($env:DURATION)) { "30s" } else { $env:DURATION }
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$statsOutput = Join-Path $resultsDir "docker-stats-full-suite-$timestamp.txt"

function Convert-DurationToSeconds {
    param([string]$Value)

    if ($Value -match '^\s*(\d+)\s*s\s*$') {
        return [int]$Matches[1]
    }

    if ($Value -match '^\s*(\d+)\s*m\s*$') {
        return [int]$Matches[1] * 60
    }

    return 30
}

$durationSeconds = Convert-DurationToSeconds -Value $Duration
$benchmarkCountPerVu = if ($SkipGrpc) { 6 } else { 9 }
$estimatedSeconds = [Math]::Max(60, ($durationSeconds + 5) * $Vus.Count * $benchmarkCountPerVu)

Write-Host "Starting full IoTFarmBench evaluation..."
Write-Host "Duration per benchmark: $Duration"
Write-Host "VUs: $($Vus -join ', ')"
Write-Host "Stats output: $statsOutput"

$statsJob = Start-Job -ScriptBlock {
    param($ScriptPath, $DurationSeconds, $IntervalSeconds, $OutputFile)
    & $ScriptPath `
        -DurationSeconds $DurationSeconds `
        -IntervalSeconds $IntervalSeconds `
        -OutputFile $OutputFile `
        -Label "full-evaluation"
} -ArgumentList (Join-Path $PSScriptRoot "docker-stats/collect-docker-stats.ps1"), $estimatedSeconds, $StatsIntervalSeconds, $statsOutput

try {
    & (Join-Path $PSScriptRoot "run-benchmarks.ps1") `
        -Vus $Vus `
        -Duration $Duration `
        -NetworkName $NetworkName `
        -SkipGrpc:$SkipGrpc
} finally {
    Write-Host "Stopping Docker stats collection..."
    Stop-Job -Job $statsJob -ErrorAction SilentlyContinue
    Receive-Job -Job $statsJob -ErrorAction SilentlyContinue
    Remove-Job -Job $statsJob -Force -ErrorAction SilentlyContinue
}

Write-Host "Full evaluation complete. Results are in $resultsDir"
Write-Host "Response sizes must be measured manually in Postman Console according to docs/postman-response-size.md."
