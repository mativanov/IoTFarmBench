# IoTFarmBench Evaluation Report

This report covers the performance evaluation required in section 4 of the university specification. The system compares REST, GraphQL, and gRPC over the same PostgreSQL database and the same Smart Farming IoT dataset.

## 1. k6 Load Testing

The benchmark suite is implemented in `tests/k6/` and executed with `tests/run-benchmarks.ps1`. The suite runs all three required scenarios for 10, 100, and 500 virtual users.

The table includes maximum latency because a few runs contain large outliers. In those cases, average latency can be greater than p95 latency.

| Protocol | Scenario | VUs | Avg latency | p95 latency | Max latency | RPS |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| REST | A - Ingestion | 10 | 27.63 ms | 15.28 ms | 536.51 ms | 9.72 |
| REST | A - Ingestion | 100 | 28.71 ms | 91.78 ms | 591.18 ms | 95.74 |
| REST | A - Ingestion | 500 | 70.67 ms | 145.26 ms | 1.58 s | 454.20 |
| GraphQL | A - Ingestion | 10 | 29.89 ms | 41.53 ms | 526.69 ms | 9.69 |
| GraphQL | A - Ingestion | 100 | 29.77 ms | 135.86 ms | 504.60 ms | 95.52 |
| GraphQL | A - Ingestion | 500 | 933.45 ms | 1.39 s | 8.29 s | 252.03 |
| gRPC | A - Ingestion | 10 | 72.88 ms | 195.26 ms | 1.23 s | 9.23 |
| gRPC | A - Ingestion | 100 | 50.03 ms | 164.11 ms | 811.15 ms | 92.25 |
| gRPC | A - Ingestion | 500 | 1.11 s | 2.27 s | 6.76 s | 225.53 |
| REST | B - Selective Monitoring | 10 | 3.65 ms | 5.12 ms | 12.99 ms | 9.95 |
| REST | B - Selective Monitoring | 100 | 3.74 ms | 6.06 ms | 109.63 ms | 99.27 |
| REST | B - Selective Monitoring | 500 | 11.04 ms | 30.94 ms | 316.17 ms | 487.81 |
| GraphQL | B - Selective Monitoring | 10 | 11.06 ms | 46.38 ms | 126.53 ms | 9.86 |
| GraphQL | B - Selective Monitoring | 100 | 12.16 ms | 23.66 ms | 328.81 ms | 98.00 |
| GraphQL | B - Selective Monitoring | 500 | 571.15 ms | 845.38 ms | 3.42 s | 310.16 |
| gRPC | B - Selective Monitoring | 10 | 5.66 ms | 9.87 ms | 53.38 ms | 9.85 |
| gRPC | B - Selective Monitoring | 100 | 11.38 ms | 36.30 ms | 318.33 ms | 96.14 |
| gRPC | B - Selective Monitoring | 500 | 795.87 ms | 1.32 s | 3.00 s | 265.44 |
| REST | C - Heavy Querying | 10 | 7.79 ms | 35.18 ms | 37.25 ms | 9.91 |
| REST | C - Heavy Querying | 100 | 17.91 ms | 17.72 ms | 482.12 ms | 97.31 |
| REST | C - Heavy Querying | 500 | 5.90 s | 7.21 s | 9.32 s | 66.79 |
| GraphQL | C - Heavy Querying | 10 | 9.20 ms | 21.04 ms | 75.82 ms | 9.89 |
| GraphQL | C - Heavy Querying | 100 | 17.33 ms | 27.48 ms | 163.72 ms | 97.81 |
| GraphQL | C - Heavy Querying | 500 | 572.28 ms | 795.74 ms | 3.91 s | 309.72 |
| gRPC | C - Heavy Querying | 10 | 42.82 ms | 49.80 ms | 1.02 s | 9.46 |
| gRPC | C - Heavy Querying | 100 | 27.32 ms | 23.40 ms | 652.93 ms | 95.25 |
| gRPC | C - Heavy Querying | 500 | 1.66 s | 10.17 s | 16.69 s | 161.32 |

## 2. Postman Response Size

Response size was measured manually in Postman, as required by the specification. REST and GraphQL values were read from Postman response size/body output. gRPC values were read in the Postman gRPC environment from the displayed response message payload.

| Protocol | Scenario | Response size |
| --- | --- | ---: |
| REST | A - High-Frequency Ingestion | 463 B |
| REST | B - Selective Monitoring | 4674 B |
| REST | C - Heavy Querying | 402 B |
| GraphQL | A - High-Frequency Ingestion | 205 B |
| GraphQL | B - Selective Monitoring | 4700 B |
| GraphQL | C - Heavy Querying | 156 B |
| gRPC | A - High-Frequency Ingestion | 524 B |
| gRPC | B - Selective Monitoring | 4674 B |
| gRPC | C - Heavy Querying | 487 B |

Note: Postman displays decoded gRPC messages through a JSON interface, so the gRPC values are the payload visible in Postman, not a Wireshark-level HTTP/2 frame measurement.

## 3. Docker Stats

CPU/RAM usage was collected with `docker stats` during every 500 VU benchmark run. Each run has a separate raw result file in `tests/results/docker-stats-<protocol>-<scenario>-500vu.txt`.

The table shows peak CPU and peak RAM for the active service container and PostgreSQL during the same test. Docker CPU can exceed 100% because it aggregates usage across CPU cores.

| Protocol | Scenario | Active service CPU peak | Active service RAM peak | PostgreSQL CPU peak | PostgreSQL RAM peak |
| --- | --- | ---: | ---: | ---: | ---: |
| REST | A - Ingestion | 539.36% | 372.1 MiB | 144.56% | 263.8 MiB |
| REST | B - Selective Monitoring | 180.84% | 416.7 MiB | 62.85% | 268.5 MiB |
| REST | C - Heavy Querying | 32.74% | 231.0 MiB | 1183.36% | 590.1 MiB |
| GraphQL | A - Ingestion | 138.57% | 105.7 MiB | 369.05% | 319.9 MiB |
| GraphQL | B - Selective Monitoring | 135.17% | 106.0 MiB | 370.83% | 319.4 MiB |
| GraphQL | C - Heavy Querying | 133.12% | 106.5 MiB | 369.55% | 319.5 MiB |
| gRPC | A - Ingestion | 605.98% | 577.3 MiB | 513.73% | 325.0 MiB |
| gRPC | B - Selective Monitoring | 325.30% | 611.4 MiB | 538.28% | 322.5 MiB |
| gRPC | C - Heavy Querying | 312.11% | 609.5 MiB | 1407.81% | 572.9 MiB |

## 4. Analysis

Scenario A shows REST with the best throughput at 500 VU, around 454 RPS, and lower average latency than GraphQL and gRPC in this run.

Scenario B highlights selective data retrieval. REST performs very well because it has a dedicated selective endpoint. GraphQL provides client-driven field selection but shows higher overhead at 500 VU. gRPC has better throughput than GraphQL in this run, but with significant CPU and RAM pressure on the gRPC service container.

Scenario C is dominated by database aggregation cost. PostgreSQL reaches 1183.36% CPU in the REST heavy-querying run and 1407.81% in the gRPC heavy-querying run, so protocol cost must be interpreted together with database pressure.

The Docker stats results show that serialization/deserialization cost cannot be isolated from database access with container-level metrics alone. REST ingestion mainly stresses the REST service container, heavy querying stresses PostgreSQL, and gRPC shows the highest service-container CPU/RAM peaks in this implementation.

## 5. Limitations

The measurements were executed locally with Docker Desktop, so results depend on host CPU, RAM, Docker resource limits, and background system load. Docker stats reports container-level CPU and memory usage; it does not isolate only serialization/deserialization cost.

## 6. Conclusion

REST is the simplest to expose and debug and performs very well when endpoints are shaped for the scenario. GraphQL is strongest when clients need flexible field selection, although resolver and query parsing overhead appear at high load. gRPC remains suitable for typed service-to-service communication and performs well in selective monitoring, while heavy querying shows that database access can dominate protocol differences.
