# Izvestaj o merenju performansi sistema IoTFarmBench

## 1. Uvod

IoTFarmBench poredi REST, GraphQL i gRPC komunikaciju u istom IoT mikroservisnom sistemu za pametnu poljoprivredu. Sva tri servisa pristupaju istoj PostgreSQL bazi i istom Smart Farming dataset-u, pa rezultati pokazuju odnos izmedju protokola, implementacije servisa i opterecenja baze.

Ovaj izvestaj je namerno kriticki: ne pretpostavlja da je jedan protokol uvek najbolji i posebno razdvaja ono sto je direktno uporedivo od onoga sto je aproksimacija ili vrednost prikazana u alatu.

## 2. Sta je tacno mereno

Merena su tri scenarija iz specifikacije:

- Scenario A - High-Frequency Ingestion: jedan zahtev po iteraciji kreira novo senzorsko merenje.
- Scenario B - Selective Monitoring: klijent trazi 100 poslednjih merenja, ali samo `temperatureC` i `humidityPercent`.
- Scenario C - Heavy Querying: svaka iteracija izvrsava jednu analiticku operaciju, naizmenicno summary agregaciju i agregaciju po regionu.

k6 meri prosecnu latenciju, p95 latenciju, maksimalnu latenciju i `successful_requests` RPS. U tabeli se koristi uspesni RPS, a ne samo broj poslatih HTTP/gRPC zahteva.

## 3. Koja merenja su direktno uporediva

Latencija i RPS iz k6 testova su direktno uporedivi kada su ispunjena tri uslova: isti broj virtuelnih korisnika, isti scenario i ista logicka operacija po iteraciji. Zbog toga su k6 skripte korigovane tako da:

- GraphQL heavy-querying trazi ista analiticka polja kao REST/gRPC.
- GraphQL provere uspesnosti proveravaju `data` i odsustvo `errors`, a ne samo HTTP status.
- gRPC ne otvara i zatvara konekciju u svakoj iteraciji, vec koristi konekciju po VU.
- Sva tri protokola u Scenariju C rade jednu logicku analiticku operaciju po iteraciji.

## 4. Aproksimacije i Postman vrednosti

REST i GraphQL velicine odgovora su JSON response-body vrednosti iz Postman Console prikaza. gRPC vrednosti su ocitane u Postman gRPC okruzenju kao dekodovana poruka.

gRPC veličina prikazana u tabeli predstavlja veličinu dekodovanog odgovora u Postman gRPC okruženju, a ne niskonivojsko Wireshark merenje sirovih HTTP/2/Protobuf frame-ova.

Zbog toga gRPC response-size tabela ne sme da se tumaci kao dokaz raw Protobuf wire-size prednosti ili mane. Za takav zakljucak potreban je Wireshark ili slican niskonivojski capture.

## 5. k6 rezultati

| Protokol | Scenario | VU | Prosecna latencija | p95 latencija | Max latencija | Uspesni RPS | Uspesnost provera |
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

Grafici za prezentaciju:

- `docs/charts/latency-p95-500vu.png`
- `docs/charts/rps-500vu.png`

## 6. Velicina odgovora

| Protokol | Scenario | Velicina odgovora | Izvor |
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

Grafik: `docs/charts/response-size.png`

Selektivni gRPC odgovor logicki vraca trazene vrednosti, ali je poruka tipizirana kao `SelectiveReadingMessage`. U Postman decoded prikazu struktura poruke i proto3 default vrednosti mogu uciniti prikaz vecim od REST/GraphQL JSON odgovora. To ne znaci da je raw Protobuf payload nuzno veci; znaci samo da Postman prikaz nije isto sto i raw wire-size.

## 7. Zasto je heavy querying cesto database-bound

U Scenariju C sva tri protokola izvrsavaju agregacije nad velikim brojem istorijskih zapisa. Pri 500 VU PostgreSQL CPU peak prelazi 1200% kod sva tri protokola. To pokazuje da dominantan trosak nije samo format poruke, vec konkurentno izvrsavanje SQL agregacija, pristup indeksima, planiranje upita i rad baze.

Zato raniji zakljucak da je GraphQL dramaticno brzi od REST-a u heavy-querying scenariju nije bio dovoljno opravdan. Nakon izjednacavanja polja i provera, REST, GraphQL i gRPC imaju slican red velicine p95 latencije u Scenariju C.

## 8. Zasto protokol nije uvek dominantan faktor

Kod ingestion scenarija vazni su validacija, upis u bazu, connection pooling i transakcija. Kod selective monitoringa vazni su oblik endpointa i broj vracenih polja. Kod heavy queryinga baza dominira. Zbog toga izbor protokola treba vezati za konkretan scenario:

- REST je vrlo efikasan kada postoje namenski endpointi.
- GraphQL je koristan kada klijentima treba fleksibilan izbor polja.
- gRPC je pogodan za tipiziranu internu komunikaciju, ali mora se pravilno koristiti konekcija.

## 9. Sumnjivi i outlier rezultati

Raniji 500 VU rezultati su bili sumnjivi iz tri razloga. Prvo, neki izvestaji su koristili ukupan request rate iako je `successful_requests` bio 0/s ili nizak. Drugo, GraphQL heavy-querying je vracao manje polja nego REST/gRPC. Trece, gRPC k6 skripte su otvarale konekciju po iteraciji, sto je stvaralo nepotreban overhead.

Tokom rerun-a je otkriven i problem sa PostgreSQL konekcijama: REST/gRPC su koristili podrazumevani Npgsql pool od 100 konekcija po servisu, dok PostgreSQL ima ogranicen broj konekcija. Dodat je `DB_POOL_MAX` limit kako se ne bi merilo iscrpljivanje konekcija umesto performansi protokola.

## 10. Docker stats analiza

| Protokol | Scenario | Aktivni servis CPU peak | Aktivni servis RAM peak | PostgreSQL CPU peak | PostgreSQL RAM peak |
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

Grafici:

- `docs/charts/docker-cpu-500vu.png`
- `docs/charts/docker-ram-500vu.png`

CPU procenat veci od 100% znaci da Docker sabira upotrebu vise CPU jezgara. Peak vrednosti ne predstavljaju prosecno opterecenje celog testa, vec najvecu zabelezenu vrednost u uzorcima.

## 11. Trosak serijalizacije i deserijalizacije

Docker stats ne izoluje samo serijalizaciju/deserijalizaciju. U istom CPU broju nalaze se HTTP/gRPC obrada, JSON ili Protobuf kodiranje, pristup bazi, cekanje konekcija, rad runtime-a, garbage collection i SQL agregacije.

Ipak, rezultati daju smernice. GraphQL ima dodatni trosak parsiranja upita i resolver sloja. gRPC smanjuje deo overhead-a formatom poruke, ali servis i dalje placa cenu mapiranja tipiziranih poruka, HTTP/2 obrade i rada sa bazom. REST ostaje vrlo konkurentan kada je endpoint precizno oblikovan.

## 12. Zakljucak

Sekcija 4 specifikacije je sada predstavljena korektnije: postoje k6 merenja za 10, 100 i 500 VU, Postman response-size tabela jasno razlikuje JSON body od Postman decoded gRPC prikaza, a Docker stats rezultati su povezani sa stvarnim 500 VU testovima.

Najvazniji zakljucak je da nema univerzalnog pobednika. REST je praktican i brz za jasne endpoint-e, GraphQL daje fleksibilnost i smanjuje over-fetching, a gRPC je dobar za tipiziranu internu komunikaciju. U heavy-querying scenariju baza dominira, pa protokol nije glavni faktor performansi.
