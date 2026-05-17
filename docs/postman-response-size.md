# Postman response-size merenje

Ovaj fajl opisuje zvanicni postupak za merenje velicine odgovora iz tacke 4b specifikacije. Vrednosti iz Postman-a treba tumaciti pazljivo:

- REST i GraphQL: JSON response body / Postman Console prikaz.
- gRPC: dekodovana response poruka u Postman gRPC okruzenju.
- gRPC vrednosti nisu niskonivojsko Wireshark merenje sirovih HTTP/2/Protobuf frame-ova.

gRPC veličina prikazana u tabeli predstavlja veličinu dekodovanog odgovora u Postman gRPC okruženju, a ne niskonivojsko Wireshark merenje sirovih HTTP/2/Protobuf frame-ova.


## Priprema

1. Pokrenuti sistem:

```powershell
docker compose up --build -d
```

2. Ako je baza prazna, ucitati dataset:

```powershell
docker compose run --rm importer
```

3. U Postmanu otvoriti Console pre slanja zahteva i ocitati velicinu odgovora.

## REST zahtevi

Base URL:

```text
http://localhost:5000
```

Scenario A - High-Frequency Ingestion:

```http
POST http://localhost:5000/api/readings
Content-Type: application/json
```

```json
{
  "sensorId": "postman-rest-sensor-001",
  "farmId": "postman-farm-001",
  "region": "Central",
  "latitude": 44.8125,
  "longitude": 20.4612,
  "timestamp": "2026-05-16T12:00:00Z",
  "cropType": "Wheat",
  "temperatureC": 24.5,
  "humidityPercent": 62.1,
  "soilMoisturePercent": 38.4,
  "soilPh": 6.8,
  "rainfallMm": 3.2,
  "sunlightHours": 8.5,
  "ndviIndex": 0.74
}
```

Scenario B - Selective Monitoring:

```http
GET http://localhost:5000/api/readings/selective?fields=temperatureC,humidityPercent&limit=100
```

Scenario C - Heavy Querying:

```http
GET http://localhost:5000/api/analytics/summary
```

## GraphQL zahtevi

URL:

```text
http://localhost:5002/
```

Scenario A - High-Frequency Ingestion:

```graphql
mutation {
  createReading(input: {
    sensorId: "postman-graphql-sensor-001"
    farmId: "postman-farm-001"
    region: "Central"
    latitude: 44.8125
    longitude: 20.4612
    timestamp: "2026-05-16T12:00:00Z"
    cropType: "Wheat"
    temperatureC: 24.5
    humidityPercent: 62.1
    soilMoisturePercent: 38.4
    soilPh: 6.8
    rainfallMm: 3.2
    sunlightHours: 8.5
    ndviIndex: 0.74
  }) {
    id
    deviceId
    timestamp
    temperatureC
    humidityPercent
  }
}
```

Scenario B - Selective Monitoring:

```graphql
query {
  readings(limit: 100) {
    temperatureC
    humidityPercent
  }
}
```

Scenario C - Heavy Querying:

```graphql
query {
  analyticsSummary {
    count
    avgTemperatureC
    avgHumidityPercent
    avgSoilMoisturePercent
    avgSoilPh
    avgRainfallMm
    avgSunlightHours
    avgNdviIndex
    avgYieldKgPerHectare
    minTimestamp
    maxTimestamp
  }
}
```

## gRPC zahtevi

Target:

```text
localhost:5001
```

Proto fajl:

```text
services/grpc-service/Protos/farm_benchmark.proto
```

Servis:

```text
iotfarmbench.grpc.FarmBenchmarkService
```

Scenario A - High-Frequency Ingestion:

Method:

```text
CreateReading
```

Message:

```json
{
  "sensor_id": "postman-grpc-sensor-001",
  "farm_id": "postman-farm-001",
  "region": "Central",
  "latitude": 44.8125,
  "longitude": 20.4612,
  "timestamp": "2026-05-16T12:00:00Z",
  "crop_type": "Wheat",
  "temperature_c": 24.5,
  "humidity_percent": 62.1,
  "soil_moisture_percent": 38.4,
  "soil_ph": 6.8,
  "rainfall_mm": 3.2,
  "sunlight_hours": 8.5,
  "ndvi_index": 0.74
}
```

Scenario B - Selective Monitoring:

Method:

```text
GetSelectiveReadings
```

Message:

```json
{
  "fields": ["temperatureC", "humidityPercent"],
  "limit": 100
}
```

Scenario C - Heavy Querying:

Method:

```text
GetAnalyticsSummary
```

Message:

```json
{}
```

## Tabela za finalni izvestaj

| Protokol | Scenario | Velicina odgovora | Izvor merenja |
| --- | --- | ---: | --- |
| REST | A - High-Frequency Ingestion | 463 B | Postman Console, JSON body |
| REST | B - Selective Monitoring | 4674 B | Postman Console, JSON body |
| REST | C - Heavy Querying | 402 B | Postman Console, JSON body |
| GraphQL | A - High-Frequency Ingestion | 205 B | Postman Console, JSON body |
| GraphQL | B - Selective Monitoring | 4700 B | Postman Console, JSON body |
| GraphQL | C - Heavy Querying | 156 B | Postman Console, JSON body |
| gRPC | A - High-Frequency Ingestion | 524 B | Postman decoded gRPC message |
| gRPC | B - Selective Monitoring | oko 20 KB | Postman decoded gRPC message |
| gRPC | C - Heavy Querying | 487 B | Postman decoded gRPC message |

## Napomena za gRPC selective monitoring

`GetSelectiveReadings` logicki dobija samo trazena polja, ali odgovor je i dalje tipizirana Protobuf poruka (`SelectiveReadingMessage`) u okviru repeated liste. Postman gRPC prikazuje dekodovanu reprezentaciju poruke. Zbog toga prikaz moze biti znatno veci od ocekivanog raw binarnog payload-a, posebno ako alat prikazuje strukturu i proto3 default vrednosti.

U odbrani projekta ne treba reci da je ova vrednost "binarna Protobuf velicina". Ispravno je reci da je to Postman decoded gRPC response size.
