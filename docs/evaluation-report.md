# IoTFarmBench Evaluation Report

This report covers section 4 of the project specification: k6 load testing, Postman response-size measurement, and Docker stats CPU/RAM observation. The comparison uses the same PostgreSQL database and Smart Farming IoT dataset for REST, GraphQL, and gRPC.

Important correction: RPS in this report is **successful requests per second** from the custom `successful_requests` k6 counter, not raw HTTP/gRPC request count. Earlier runs also used a GraphQL check that treated a missing `errors` field incorrectly and gRPC scripts that opened a connection every iteration. Those scripts have been corrected.

## 1. k6 Load Testing

The benchmark suite in `tests/k6/` runs three scenarios at 10, 100, and 500 VU:

- Scenario A: high-frequency ingestion, one create operation per iteration.
- Scenario B: selective monitoring, 100 latest readings with only `temperatureC` and `humidityPercent`.
- Scenario C: heavy querying, alternating one analytics summary operation and one analytics-by-region operation.

GraphQL heavy-querying now requests the same analytics fields as the REST/gRPC responses expose, so the 500 VU comparison should no longer be read as “GraphQL is much faster because it returns less data.”

| Protocol | Scenario | VUs | Avg latency | p95 latency | Max latency | Successful RPS | Check success |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| REST | A - Ingestion | 10 | 76.03 ms | 110.04 ms | 1.61 s | 9.28 | 100.00% |
| REST | A - Ingestion | 100 | 22.66 ms | 34.91 ms | 419.51 ms | 96.92 | 100.00% |
| REST | A - Ingestion | 500 | 33.80 ms | 69.80 ms | 815.78 ms | 471.84 | 100.00% |
| REST | B - Selective Monitoring | 10 | 7.94 ms | 5.16 ms | 142.43 ms | 9.91 | 100.00% |
| REST | B - Selective Monitoring | 100 | 5.54 ms | 12.39 ms | 74.81 ms | 99.15 | 100.00% |
| REST | B - Selective Monitoring | 500 | 7.61 ms | 25.20 ms | 273.87 ms | 489.94 | 100.00% |
| REST | C - Heavy Querying | 10 | 148.27 ms | 291.99 ms | 584.26 ms | 8.58 | 100.00% |
| REST | C - Heavy Querying | 100 | 1.42 s | 2.16 s | 3.60 s | 39.77 | 100.00% |
| REST | C - Heavy Querying | 500 | 10.95 s | 14.12 s | 14.76 s | 35.83 | 100.00% |
| GraphQL | A - Ingestion | 10 | 27.75 ms | 46.96 ms | 335.84 ms | 9.71 | 100.00% |
| GraphQL | A - Ingestion | 100 | 26.45 ms | 43.30 ms | 691.82 ms | 96.18 | 100.00% |
| GraphQL | A - Ingestion | 500 | 390.40 ms | 701.39 ms | 7.11 s | 349.58 | 100.00% |
| GraphQL | B - Selective Monitoring | 10 | 9.26 ms | 13.37 ms | 156.94 ms | 9.86 | 100.00% |
| GraphQL | B - Selective Monitoring | 100 | 17.58 ms | 35.77 ms | 508.60 ms | 97.19 | 100.00% |
| GraphQL | B - Selective Monitoring | 500 | 842.06 ms | 1.26 s | 22.20 s | 262.57 | 100.00% |
| GraphQL | C - Heavy Querying | 10 | 141.56 ms | 229.22 ms | 541.13 ms | 8.71 | 100.00% |
| GraphQL | C - Heavy Querying | 100 | 1.56 s | 2.29 s | 3.56 s | 37.99 | 100.00% |
| GraphQL | C - Heavy Querying | 500 | 11.66 s | 14.68 s | 15.27 s | 33.51 | 100.00% |
| gRPC | A - Ingestion | 10 | 45.97 ms | 38.46 ms | 928.97 ms | 9.52 | 100.00% |
| gRPC | A - Ingestion | 100 | 21.94 ms | 42.50 ms | 345.72 ms | 96.89 | 100.00% |
| gRPC | A - Ingestion | 500 | 61.04 ms | 249.88 ms | 799.29 ms | 460.10 | 100.00% |
| gRPC | B - Selective Monitoring | 10 | 5.37 ms | 6.13 ms | 66.09 ms | 9.90 | 100.00% |
| gRPC | B - Selective Monitoring | 100 | 7.39 ms | 19.24 ms | 199.61 ms | 97.77 | 100.00% |
| gRPC | B - Selective Monitoring | 500 | 37.82 ms | 192.09 ms | 620.66 ms | 464.20 | 100.00% |
| gRPC | C - Heavy Querying | 10 | 132.30 ms | 191.27 ms | 413.48 ms | 8.71 | 100.00% |
| gRPC | C - Heavy Querying | 100 | 1.89 s | 2.80 s | 4.76 s | 33.44 | 100.00% |
| gRPC | C - Heavy Querying | 500 | 12.84 s | 15.80 s | 16.87 s | 27.29 | 90.58% |

Charts:

- `docs/charts/latency-p95-500vu.png`
- `docs/charts/rps-500vu.png`

## 2. Postman Response Size

REST and GraphQL sizes are JSON response-body sizes read from Postman Console. gRPC sizes are decoded-message sizes visible in the Postman gRPC environment. They are not raw Protobuf wire-size measurements.

| Protocol | Scenario | Response size | Measurement source |
| --- | --- | ---: | --- |
| REST | A - High-Frequency Ingestion | 463 B | Postman Console JSON body |
| REST | B - Selective Monitoring | 4674 B | Postman Console JSON body |
| REST | C - Heavy Querying | 402 B | Postman Console JSON body |
| GraphQL | A - High-Frequency Ingestion | 205 B | Postman Console JSON body |
| GraphQL | B - Selective Monitoring | 4700 B | Postman Console JSON body |
| GraphQL | C - Heavy Querying | 156 B | Postman Console JSON body |
| gRPC | A - High-Frequency Ingestion | 524 B | Postman decoded gRPC message |
| gRPC | B - Selective Monitoring | about 20 KB | Postman decoded gRPC message |
| gRPC | C - Heavy Querying | 487 B | Postman decoded gRPC message |

Postman displays decoded gRPC messages through a JSON-like interface. The selective gRPC response uses a typed `SelectiveReadingMessage`; even when only two logical fields are requested, decoded displays can include message structure/default-field representation that is not directly comparable with raw binary Protobuf bytes. A true raw Protobuf comparison would require Wireshark or equivalent HTTP/2 frame capture.

Chart: `docs/charts/response-size.png`

## 3. Docker Stats

Docker stats were collected during the 500 VU runs with `tests/run-benchmarks-with-docker-stats.ps1`. The table shows peak CPU/RAM for the active service container and PostgreSQL. Docker CPU can exceed 100% because it is summed across CPU cores.

| Protocol | Scenario | Active service CPU peak | Active service RAM peak | PostgreSQL CPU peak | PostgreSQL RAM peak |
| --- | --- | ---: | ---: | ---: | ---: |
| REST | A - Ingestion | 102.75% | 205.3 MiB | 122.80% | 215.7 MiB |
| REST | B - Selective Monitoring | 86.09% | 222.5 MiB | 56.64% | 217.7 MiB |
| REST | C - Heavy Querying | 17.94% | 224.1 MiB | 1259.18% | 351.8 MiB |
| GraphQL | A - Ingestion | 113.39% | 105.6 MiB | 85.08% | 298.2 MiB |
| GraphQL | B - Selective Monitoring | 133.31% | 118.2 MiB | 34.25% | 301.1 MiB |
| GraphQL | C - Heavy Querying | 21.38% | 117.4 MiB | 1238.40% | 428.6 MiB |
| gRPC | A - Ingestion | 190.28% | 357.8 MiB | 240.74% | 225.2 MiB |
| gRPC | B - Selective Monitoring | 193.98% | 594.0 MiB | 114.92% | 227.3 MiB |
| gRPC | C - Heavy Querying | 27.47% | 677.1 MiB | 1211.51% | 289.3 MiB |

Charts:

- `docs/charts/docker-cpu-500vu.png`
- `docs/charts/docker-ram-500vu.png`

## 4. Analysis

Scenario A now shows REST and gRPC with high successful throughput at 500 VU, while GraphQL is lower because mutation parsing/resolver overhead and Node.js runtime behavior become visible under load.

Scenario B shows the benefit of shaped/selective reads. REST has the lowest p95 latency with a dedicated endpoint, gRPC also performs well after connection reuse, and GraphQL pays additional query/resolver overhead despite returning only selected fields.

Scenario C is database-bound. At 500 VU, PostgreSQL CPU peaks around 1200% for all three protocols. REST, GraphQL, and gRPC have similar p95 latency in this scenario, so the earlier claim that GraphQL was dramatically faster was misleading. The main cost is the aggregation query pressure on PostgreSQL, not only protocol serialization.

Docker stats are useful for system-level comparison, but they do not isolate serialization/deserialization CPU cost by themselves. The measured CPU includes request handling, DB access, runtime scheduling, garbage collection, connection pooling, and serialization.

## 5. Limitations

Measurements were run locally in Docker Desktop and depend on host resources, Docker limits, background load, and the current database contents. The project now caps DB connection pools with `DB_POOL_MAX` to avoid measuring PostgreSQL connection exhaustion instead of protocol behavior.

The response-size table follows the project requirement to use Postman, but gRPC sizes are decoded Postman display values unless a separate raw HTTP/2/Protobuf capture is performed.

## 6. Conclusion

The corrected evaluation supports a tradeoff conclusion. REST is simple and efficient when endpoints are shaped for the use case. GraphQL is useful for flexible field selection but has overhead under high load. gRPC benefits from typed binary communication and connection reuse, but it is not automatically best when database aggregation dominates or when the implementation is stressed.
