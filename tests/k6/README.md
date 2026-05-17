# k6 Benchmarks

These scripts benchmark the three IoTFarmBench scenarios. The automated runner uses the official `grafana/k6` Docker image, so local k6 installation is not required.

- Scenario A: High-Frequency Ingestion
- Scenario B: Selective Monitoring
- Scenario C: Heavy Querying

Start the application first:

```powershell
docker compose up --build -d
```

## Automated Docker Runner

Start Docker Desktop and the application stack, then run:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1
```

The runner mounts the project into the k6 container, joins the `iotfarmbench_default` Docker network, and writes outputs to `tests/results/`.

Single-script Docker example:

```powershell
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://rest-service:5000 grafana/k6 run tests/k6/rest-selective-monitoring.js
```

## REST

```powershell
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://rest-service:5000 grafana/k6 run tests/k6/rest-ingestion.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://rest-service:5000 grafana/k6 run tests/k6/rest-selective-monitoring.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://rest-service:5000 grafana/k6 run tests/k6/rest-heavy-querying.js
```

## GraphQL

```powershell
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://graphql-service:5002 grafana/k6 run tests/k6/graphql-ingestion.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://graphql-service:5002 grafana/k6 run tests/k6/graphql-selective-monitoring.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e BASE_URL=http://graphql-service:5002 grafana/k6 run tests/k6/graphql-heavy-querying.js
```

## gRPC

The gRPC script uses `k6/net/grpc` and loads `services/grpc-service/Protos/farm_benchmark.proto`.

```powershell
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e GRPC_HOST=grpc-service:5001 grafana/k6 run tests/k6/grpc-ingestion.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e GRPC_HOST=grpc-service:5001 grafana/k6 run tests/k6/grpc-selective-monitoring.js
docker run --rm --network iotfarmbench_default -v "${PWD}:/app" -w /app -e GRPC_HOST=grpc-service:5001 grafana/k6 run tests/k6/grpc-heavy-querying.js
```

Manual grpcurl check:

```powershell
grpcurl -plaintext localhost:5001 list
```

## Custom Load With Runner

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Vus 100 -Duration "1m"
```

Defaults:

- `VUS=10`
- `DURATION=30s`
- REST inside Docker network: `BASE_URL=http://rest-service:5000`
- GraphQL inside Docker network: `BASE_URL=http://graphql-service:5002`
- gRPC inside Docker network: `GRPC_TARGET=grpc-service:5001`

## Required Project Loads

10 virtual users:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Vus 10 -Duration "1m"
```

100 virtual users:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Vus 100 -Duration "1m"
```

500 virtual users:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Vus 500 -Duration "1m"
```

## Useful k6 Output

Record the following metrics for the evaluation:

- average and p95 latency from `http_req_duration` for REST and GraphQL
- average and p95 latency from `grpc_req_duration` for gRPC
- successful request rate from the custom `successful_requests` counter rate
- failure rate from `http_req_failed` for REST/GraphQL and `request_failure_rate` for all protocols
- checks pass rate as an additional success signal

## Automated Runner

Run the complete REST, GraphQL, and gRPC benchmark suite through Docker and save outputs to `tests/results/`:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1
```

Run only 10 VU tests:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Vus 10
```

Run with a 1 minute duration:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1 -Duration "1m"
```

See `tests/README.md` for the full evaluation workflow.
