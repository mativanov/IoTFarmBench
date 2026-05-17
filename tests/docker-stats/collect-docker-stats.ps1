param(
    [int]$IntervalSeconds = 5,
    [int]$Samples = 12,
    [int]$DurationSeconds = 0,
    [string]$OutputFile = "",
    [string]$Label = ""
)

$ErrorActionPreference = "Stop"

$containers = @(
    "iotfarmbench-rest",
    "iotfarmbench-grpc",
    "iotfarmbench-graphql",
    "iotfarmbench-postgres"
)

$resultsDir = Join-Path $PSScriptRoot "..\results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputPath = if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    Join-Path $resultsDir "docker-stats-$timestamp.txt"
} else {
    $OutputFile
}

if ($DurationSeconds -gt 0) {
    $Samples = [Math]::Max(1, [Math]::Ceiling($DurationSeconds / $IntervalSeconds))
}

"IoTFarmBench Docker stats" | Out-File -FilePath $outputPath -Encoding utf8
"Started: $(Get-Date -Format o)" | Out-File -FilePath $outputPath -Encoding utf8 -Append
"DurationSeconds: $DurationSeconds" | Out-File -FilePath $outputPath -Encoding utf8 -Append
"IntervalSeconds: $IntervalSeconds" | Out-File -FilePath $outputPath -Encoding utf8 -Append
"Samples: $Samples" | Out-File -FilePath $outputPath -Encoding utf8 -Append
if (-not [string]::IsNullOrWhiteSpace($Label)) {
    "Label: $Label" | Out-File -FilePath $outputPath -Encoding utf8 -Append
}
"" | Out-File -FilePath $outputPath -Encoding utf8 -Append

for ($i = 1; $i -le $Samples; $i++) {
    "Sample $i - $(Get-Date -Format o)" | Out-File -FilePath $outputPath -Encoding utf8 -Append
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}\t{{.NetIO}}" $containers |
        Out-File -FilePath $outputPath -Encoding utf8 -Append
    "" | Out-File -FilePath $outputPath -Encoding utf8 -Append

    if ($i -lt $Samples) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}

Write-Host "Docker stats written to $outputPath"
