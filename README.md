# IoTFarmBench

IoTFarmBench is a university project for comparative analysis of synchronous communication paradigms in a Smart Farming IoT microservice system:

- REST with ASP.NET Core Web API and JSON over HTTP
- gRPC with ASP.NET Core gRPC and Protobuf over HTTP/2
- GraphQL with Node.js Apollo Server and JSON over HTTP

The system is fully containerized with Docker Compose, uses PostgreSQL for storage, imports a Smart Farming CSV dataset, and includes reproducible k6 benchmark workflows for REST, GraphQL, and gRPC.

## Architecture

```text
CSV dataset
  -> Python importer
  -> PostgreSQL
       -> REST service     :5000
       -> gRPC service     :5001
       -> GraphQL service  :5002

k6 Docker runner
  -> REST / GraphQL / gRPC benchmark scenarios

Measurement helpers
  -> k6 benchmark results
  -> Docker CPU/RAM/network stats
  -> manual Postman response-size procedure
```

## Services

| Service | Port | Description |
| --- | ---: | --- |
| PostgreSQL | 5432 | Stores devices and sensor readings |
| pgAdmin | 5050 | Database administration UI |
| REST service | 5000 | JSON HTTP API with Swagger and `/health` |
| gRPC service | 5001 | Protobuf API with gRPC reflection |
| GraphQL service | 5002 | Apollo Server with selective field querying |

## Run The System

```powershell
docker compose up --build -d
```

If the database is empty, import the dataset:

```powershell
docker compose run --rm importer
```

The CSV file belongs in `data/`. The importer automatically loads the first `.csv` file in that folder.

## API Checks

REST:

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/api/devices
curl "http://localhost:5000/api/readings?limit=10"
```

gRPC:

```powershell
grpcurl -plaintext localhost:5001 list
```

GraphQL:

Open `http://localhost:5002/` and run:

```graphql
query {
  readings(limit: 10) {
    temperatureC
    humidityPercent
  }
}
```

## Benchmark Workflow

k6 is run through Docker, so local k6 installation is not required.

Run the benchmark suite:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1
```

For the official university report, response sizes are recorded manually from Postman Console for REST and GraphQL, and from Postman gRPC Console for gRPC. Use `docs/postman-response-size.md` for the exact requests and result table.

Collect Docker CPU/RAM/network stats while benchmarks are running:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-docker-stats.ps1
```

Run the full evaluation workflow:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-full-evaluation.ps1
```

Results are stored in `tests/results/`.

## Benchmark Scenarios

- Scenario A, High-Frequency Ingestion: clients repeatedly create sensor readings.
- Scenario B, Selective Monitoring: clients request temperature and humidity only.
- Scenario C, Heavy Querying: clients request summary and by-region aggregations.

The benchmark suite supports 10, 100, and 500 virtual users.

## Documentation

- `docs/evaluation-report.md`: section 4 evaluation report for k6, Postman response size, and Docker stats
- `docs/izvestaj-projekat.md`: opis arhitekture i implementacije projekta
- `docs/izvestaj-merenja.md`: izvestaj o k6, Postman response-size i Docker stats merenjima
- `docs/postman-response-size.md`: koraci za rucno merenje velicine odgovora u Postmanu
