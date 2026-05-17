param(
    [int[]]$Vus = @(500),
    [string]$Duration = "30s",
    [string]$NetworkName = "iotfarmbench_default",
    [int]$StatsIntervalSeconds = 5,
    [switch]$SkipGrpc
)

$ErrorActionPreference = "Continue"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resultsDir = Join-Path $PSScriptRoot "results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

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
$statsSeconds = $durationSeconds + 10

$benchmarks = @(
    @{ Protocol = "rest"; Scenario = "ingestion"; Script = "tests/k6/rest-ingestion.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "rest"; Scenario = "selective-monitoring"; Script = "tests/k6/rest-selective-monitoring.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "rest"; Scenario = "heavy-querying"; Script = "tests/k6/rest-heavy-querying.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "graphql"; Scenario = "ingestion"; Script = "tests/k6/graphql-ingestion.js"; BaseUrl = "http://graphql-service:5002" },
    @{ Protocol = "graphql"; Scenario = "selective-monitoring"; Script = "tests/k6/graphql-selective-monitoring.js"; BaseUrl = "http://graphql-service:5002" },
    @{ Protocol = "graphql"; Scenario = "heavy-querying"; Script = "tests/k6/graphql-heavy-querying.js"; BaseUrl = "http://graphql-service:5002" }
)

if (-not $SkipGrpc) {
    $benchmarks += @(
        @{ Protocol = "grpc"; Scenario = "ingestion"; Script = "tests/k6/grpc-ingestion.js"; GrpcHost = "grpc-service:5001" },
        @{ Protocol = "grpc"; Scenario = "selective-monitoring"; Script = "tests/k6/grpc-selective-monitoring.js"; GrpcHost = "grpc-service:5001" },
        @{ Protocol = "grpc"; Scenario = "heavy-querying"; Script = "tests/k6/grpc-heavy-querying.js"; GrpcHost = "grpc-service:5001" }
    )
}

Write-Host "IoTFarmBench benchmark + per-test Docker stats runner"
Write-Host "Duration: $Duration"
Write-Host "VUs: $($Vus -join ', ')"
Write-Host "Docker network: $NetworkName"
Write-Host "Results: $resultsDir"

foreach ($vu in $Vus) {
    foreach ($benchmark in $benchmarks) {
        $scriptPath = Join-Path $projectRoot $benchmark.Script
        if (-not (Test-Path $scriptPath)) {
            Write-Warning "Skipping missing script: $scriptPath"
            continue
        }

        $name = "$($benchmark.Protocol)-$($benchmark.Scenario)-${vu}vu"
        $benchmarkOutput = Join-Path $resultsDir "$name.txt"
        $statsOutput = Join-Path $resultsDir "docker-stats-$name.txt"

        Write-Host "Running $name with Docker stats..."

        $statsJob = Start-Job -ScriptBlock {
            param($ScriptPath, $DurationSeconds, $IntervalSeconds, $OutputFile, $Label)
            & $ScriptPath `
                -DurationSeconds $DurationSeconds `
                -IntervalSeconds $IntervalSeconds `
                -OutputFile $OutputFile `
                -Label $Label
        } -ArgumentList (Join-Path $PSScriptRoot "docker-stats/collect-docker-stats.ps1"), $statsSeconds, $StatsIntervalSeconds, $statsOutput, $name

        try {
            $dockerArgs = @(
                "run",
                "--rm",
                "--network", $NetworkName,
                "-v", "${projectRoot}:/app",
                "-w", "/app",
                "-e", "VUS=$vu",
                "-e", "DURATION=$Duration"
            )

            if ($benchmark.Protocol -eq "grpc") {
                $dockerArgs += @("-e", "GRPC_HOST=$($benchmark.GrpcHost)", "-e", "GRPC_TARGET=$($benchmark.GrpcHost)")
            } else {
                $dockerArgs += @("-e", "BASE_URL=$($benchmark.BaseUrl)")
            }

            $dockerArgs += @("grafana/k6", "run", $benchmark.Script)
            & docker @dockerArgs *> $benchmarkOutput
        } finally {
            Stop-Job -Job $statsJob -ErrorAction SilentlyContinue
            Receive-Job -Job $statsJob -ErrorAction SilentlyContinue
            Remove-Job -Job $statsJob -Force -ErrorAction SilentlyContinue
        }

        Write-Host "Saved benchmark: $benchmarkOutput"
        Write-Host "Saved Docker stats: $statsOutput"
    }
}
