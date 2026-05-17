# Izvestaj o implementaciji projekta

## Tema projekta

Projekat IoTFarmBench implementira komparativnu analizu sinhronih komunikacionih paradigmi u IoT mikroservisnom sistemu. Poredjena su tri pristupa:

- REST
- gRPC
- GraphQL

Sistem je napravljen na primeru Smart Farming domena. Podaci predstavljaju senzorska ocitavanja sa farmi, ukljucujuci temperaturu, vlaznost, pH vrednost zemljista, kolicinu padavina, broj sati sunceve svetlosti, NDVI indeks, prinos i status bolesti useva.

## Arhitektura sistema

Sistem je podeljen na vise kontejnerizovanih komponenti:

- PostgreSQL baza podataka
- Python importer za CSV dataset
- REST servis u ASP.NET Core tehnologiji
- gRPC servis u ASP.NET Core tehnologiji
- GraphQL servis u Node.js/Apollo Server tehnologiji
- k6 testovi za merenje performansi
- pomocne skripte za merenje velicine odgovora i Docker stats metrika

Sve komponente se pokrecu pomocu Docker Compose-a. Time je obezbedjeno da se sistem moze reproducibilno pokrenuti i testirati u istom okruzenju.

## Baza podataka

Baza je PostgreSQL i inicijalizuje se skriptom:

```text
database/init/001_create_schema.sql
```

Model sadrzi dve glavne tabele:

- `devices`: cuva jedinstvene IoT uredjaje, odnosno senzore
- `sensor_readings`: cuva vremenski serijalizovana senzorska ocitavanja

Tabela `sensor_readings` sadrzi timestamp, ID uredjaja i vise senzorskih vrednosti. To ispunjava zahtev da dataset bude vremenski serijalizovan i da ima vise razlicitih senzorskih podataka.

Baza je optimizovana indeksima:

- indeks po `device_id` i `timestamp`
- indeks po `timestamp`
- indeks po `sensor_id`
- indeks po `region`
- indeks po `crop_type`

Ovi indeksi podrzavaju tipicne IoT upite: poslednja ocitavanja uredjaja, filtriranje po vremenu, filtriranje po regionu i agregacije po tipu useva.

## Import dataset-a

CSV dataset se nalazi u folderu:

```text
data/
```

Importer je implementiran u Python-u:

```text
importer/import_dataset.py
```

Importer radi sledece:

1. Cita CSV fajl.
2. Validira da kolone odgovaraju ocekivanoj strukturi.
3. Parsira numericke kolone, datume i timestamp.
4. Upisuje jedinstvene senzore u tabelu `devices`.
5. Upisuje senzorska ocitavanja u tabelu `sensor_readings`.

Importer se pokrece kroz Docker Compose:

```powershell
docker compose run --rm importer
```

## REST servis

REST servis je implementiran u ASP.NET Core Web API tehnologiji.

Lokacija:

```text
services/rest-service/
```

Servis koristi JSON format i izlozen je na portu 5000. Implementirani su endpointi za:

- pregled uredjaja
- pregled senzorskih ocitavanja
- kreiranje novog ocitavanja
- selektivno citanje samo trazenih polja
- analiticke agregacije
- health proveru

REST servis ima ukljucen Swagger/OpenAPI, sto ispunjava zahtev za OpenAPI dokumentacijom.

Primeri REST ruta:

```text
GET /api/devices
GET /api/readings?limit=100
POST /api/readings
GET /api/readings/selective?fields=temperatureC,humidityPercent&limit=100
GET /api/analytics/summary
GET /api/analytics/by-region
```

## gRPC servis

gRPC servis je implementiran u ASP.NET Core gRPC tehnologiji.

Lokacija:

```text
services/grpc-service/
```

Servis koristi Protobuf definicije u fajlu:

```text
services/grpc-service/Protos/farm_benchmark.proto
```

U `.proto` fajlu je definisan `FarmBenchmarkService` sa RPC metodama za:

- citanje uredjaja
- citanje senzorskih ocitavanja
- kreiranje novog ocitavanja
- selektivno citanje
- analiticke agregacije

gRPC servis radi preko HTTP/2 i izlozen je na portu 5001. Ukljucena je i gRPC reflection podrska, sto omogucava testiranje pomocu `grpcurl`.

## GraphQL servis

GraphQL servis je implementiran u Node.js tehnologiji pomocu Apollo Server-a.

Lokacija:

```text
services/graphql-service/
```

Servis je izlozen na portu 5002. GraphQL schema definise tipove za uredjaje, senzorska ocitavanja i analiticke rezultate.

Glavna prednost GraphQL servisa u ovom projektu je selective monitoring: klijent moze traziti samo polja koja su mu potrebna.

Primer GraphQL upita:

```graphql
query {
  readings(limit: 100) {
    temperatureC
    humidityPercent
  }
}
```

Time se izbegava over-fetching bez potrebe za dodavanjem posebnog endpointa za svaku kombinaciju polja.

## Docker Compose

Sistem se pokrece komandom:

```powershell
docker compose up --build -d
```

Docker Compose konfiguracija pokrece:

- PostgreSQL
- pgAdmin
- REST servis
- gRPC servis
- GraphQL servis
- importer kao pomocni alat

Svaki servis koristi iste parametre za pristup bazi kroz environment promenljive. PostgreSQL ima healthcheck, a API servisi cekaju da baza bude spremna.

## k6 benchmark testovi

k6 testovi se nalaze u folderu:

```text
tests/k6/
```

Implementirana su tri scenarija za sva tri protokola.

Scenario A, High-Frequency Ingestion:

- REST: `rest-ingestion.js`
- GraphQL: `graphql-ingestion.js`
- gRPC: `grpc-ingestion.js`

Scenario B, Selective Monitoring:

- REST: `rest-selective-monitoring.js`
- GraphQL: `graphql-selective-monitoring.js`
- gRPC: `grpc-selective-monitoring.js`

Scenario C, Heavy Querying:

- REST: `rest-heavy-querying.js`
- GraphQL: `graphql-heavy-querying.js`
- gRPC: `grpc-heavy-querying.js`

Benchmark testovi se pokrecu pomocu:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-benchmarks.ps1
```

Skripta pokrece testove za 10, 100 i 500 virtuelnih korisnika. Rezultati se cuvaju u:

```text
tests/results/
```

## Merenje velicine odgovora

Velicina odgovora meri se rucno u Postmanu, u skladu sa specifikacijom projekta. REST i GraphQL odgovori se mere kroz Postman Console, dok se gRPC odgovor meri kroz Postman gRPC Console.

Tacni zahtevi i tabela za upis rezultata nalaze se u:

```text
docs/postman-response-size.md
```

U projektu nema skriptovanog response-size merenja, jer `grpcurl` JSON izlaz i slicne aproksimacije ne predstavljaju isto sto i Postman gRPC/Protobuf merenje trazeno specifikacijom.

## Merenje CPU i RAM resursa

Zauzece resursa meri se pomocu Docker stats komande.

Skripta:

```powershell
powershell -ExecutionPolicy Bypass -File tests/run-docker-stats.ps1
```

Prikupljaju se:

- CPU procenat
- zauzece memorije
- procenat memorije
- mrežni I/O

Za proveru je sacuvan Docker stats rezultat tokom 500 VU gRPC selective-monitoring testa:

```text
tests/results/docker-stats-grpc-selective-500vu.txt
```

## Pokrivenost specifikacije

Projekat ispunjava zahteve specifikacije:

- koristi IoT dataset sa timestamp kolonom i vise senzorskih vrednosti
- ima PostgreSQL bazu optimizovanu indeksima po vremenu i uredjaju
- implementira tri odvojena mikroservisa: REST, gRPC i GraphQL
- koristi bar dve tehnologije: ASP.NET Core i Node.js, uz Python importer
- REST servis ima Swagger/OpenAPI
- gRPC servis ima `.proto` definicije
- GraphQL servis omogucava selekciju specificnih polja
- sistem je kontejnerizovan pomocu Docker Compose-a
- postoje k6 testovi za 10, 100 i 500 virtuelnih korisnika
- pokrivena su sva tri scenarija: ingestion, selective monitoring i heavy querying
- izmerene su latencije, p95, RPS i greske
- izmerena je velicina odgovora za REST, GraphQL i gRPC
- prikupljen je Docker stats izvestaj za CPU/RAM analizu

## Zakljucak

IoTFarmBench predstavlja kompletnu implementaciju benchmark sistema za poredjenje REST, gRPC i GraphQL komunikacije u IoT mikroservisnom okruzenju. Sistem je dovoljno modularan da se svaki protokol testira nad istom bazom i istim podacima, sto omogucava fer poredjenje performansi.

REST je najjednostavniji za upotrebu i debagovanje. GraphQL je najfleksibilniji za klijente koji traze razlicite skupove polja. gRPC je najpogodniji za internu komunikaciju i tipizirane ugovore izmedju servisa.
