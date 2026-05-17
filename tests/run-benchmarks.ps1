param(
    [int[]]$Vus = @(10, 100, 500),
    [string]$Duration = "",
    [string]$NetworkName = "iotfarmbench_default",
    [switch]$SkipGrpc
)

$ErrorActionPreference = "Continue"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resultsDir = Join-Path $PSScriptRoot "results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

if ([string]::IsNullOrWhiteSpace($Duration)) {
    $Duration = if ([string]::IsNullOrWhiteSpace($env:DURATION)) { "30s" } else { $env:DURATION }
}

$benchmarks = @(
    @{ Protocol = "rest"; Scenario = "ingestion"; Script = "tests/k6/rest-ingestion.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "rest"; Scenario = "selective-monitoring"; Script = "tests/k6/rest-selective-monitoring.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "rest"; Scenario = "heavy-querying"; Script = "tests/k6/rest-heavy-querying.js"; BaseUrl = "http://rest-service:5000" },
    @{ Protocol = "graphql"; Scenario = "ingestion"; Script = "tests/k6/graphql-ingestion.js"; BaseUrl = "http://graphql-service:5002" },
    @{ Protocol = "graphql"; Scenario = "selective-monitoring"; Script = "tests/k6/graphql-selective-monitoring.js"; BaseUrl = "http://graphql-service:5002" },
    @{ Protocol = "graphql"; Scenario = "heavy-querying"; Script = "tests/k6/graphql-heavy-querying.js"; BaseUrl = "http://graphql-service:5002" }
)

$grpcBenchmarks = @(
    @{ Protocol = "grpc"; Scenario = "ingestion"; Script = "tests/k6/grpc-ingestion.js"; GrpcHost = "grpc-service:5001" },
    @{ Protocol = "grpc"; Scenario = "selective-monitoring"; Script = "tests/k6/grpc-selective-monitoring.js"; GrpcHost = "grpc-service:5001" },
    @{ Protocol = "grpc"; Scenario = "heavy-querying"; Script = "tests/k6/grpc-heavy-querying.js"; GrpcHost = "grpc-service:5001" }
)

if (-not $SkipGrpc) {
    foreach ($grpcBenchmark in $grpcBenchmarks) {
        if (Test-Path (Join-Path $projectRoot $grpcBenchmark.Script)) {
            $benchmarks += $grpcBenchmark
        }
    }
}

Write-Host "IoTFarmBench benchmark runner"
Write-Host "Duration: $Duration"
Write-Host "VUs: $($Vus -join ', ')"
Write-Host "Docker network: $NetworkName"
Write-Host "Results: $resultsDir"

foreach ($vu in $Vus) {
    foreach ($benchmark in $benchmarks) {
        $scriptPath = Join-Path $projectRoot $benchmark.Script
        $outputFile = Join-Path $resultsDir "$($benchmark.Protocol)-$($benchmark.Scenario)-${vu}vu.txt"

        if (-not (Test-Path $scriptPath)) {
            Write-Warning "Skipping missing script: $scriptPath"
            continue
        }

        Write-Host "Running $($benchmark.Protocol) $($benchmark.Scenario) with $vu VUs..."

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

            & docker @dockerArgs *> $outputFile
            $exitCode = $LASTEXITCODE
            if ($exitCode -eq 0) {
                Write-Host "Saved $outputFile"
            } else {
                Write-Warning "Benchmark failed with exit code $exitCode. Output saved to $outputFile"
            }
        } catch {
            "Benchmark runner error: $($_.Exception.Message)" | Out-File -FilePath $outputFile -Encoding utf8 -Append
            Write-Warning "Benchmark threw an error. Output saved to $outputFile"
        }
    }
}

Write-Host "Benchmark run complete."
