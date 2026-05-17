# Docker Stats Collection

Use this helper to sample CPU, memory usage, memory percentage, and network I/O during benchmarks.

The monitored containers are:

- `iotfarmbench-rest`
- `iotfarmbench-grpc`
- `iotfarmbench-graphql`
- `iotfarmbench-postgres`

Run in a separate PowerShell terminal while k6 is running:

```powershell
.\tests\docker-stats\collect-docker-stats.ps1
```

Optional parameters:

```powershell
.\tests\docker-stats\collect-docker-stats.ps1 -IntervalSeconds 2 -Samples 30
```

Duration/output mode:

```powershell
.\tests\docker-stats\collect-docker-stats.ps1 -DurationSeconds 120 -IntervalSeconds 5 -OutputFile .\tests\results\docker-stats-manual.txt
```

The script writes timestamped output to `tests/results/`.

For the required full-suite workflow, run:

```powershell
.\tests\run-full-evaluation.ps1
```

That starts Docker stats collection in the background, runs the k6 benchmark suite, stops collection, and writes `tests/results/docker-stats-full-suite-<timestamp>.txt`.
