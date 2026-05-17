# Izvestaj o merenju performansi sistema IoTFarmBench

## 1. Uvod

Cilj projekta IoTFarmBench je uporedna analiza tri sinhrona komunikaciona pristupa u mikroservisnom IoT sistemu za pametnu poljoprivredu. Porede se REST, GraphQL i gRPC komunikacija nad istim PostgreSQL skladistem podataka i istim Smart Farming dataset-om.

REST koristi JSON preko HTTP-a. GraphQL takodje koristi JSON preko HTTP-a, ali omogucava klijentu da izabere samo potrebna polja. gRPC koristi Protobuf poruke preko HTTP/2 i namenjen je efikasnoj tipiziranoj komunikaciji izmedju servisa.

## 2. Metodologija

Merenje je podeljeno na tri dela:

- k6 load testing za latenciju, p95 latenciju, maksimalnu latenciju i RPS.
- Postman merenje velicine odgovora za REST, GraphQL i gRPC.
- Docker stats pracenje CPU i RAM opterecenja kontejnera tokom k6 testova.

k6 testovi se nalaze u `tests/k6/`, a pokrecu se kroz `tests/run-benchmarks.ps1`. Testirani su nivoi opterecenja od 10, 100 i 500 virtuelnih korisnika. U tabeli je dodat i `max` zato sto nekoliko testova ima ekstremne outlier-e, pa prosecna latencija moze biti veca od p95 latencije.

Merenje velicine odgovora radjeno je rucno u Postmanu. REST i GraphQL vrednosti su ocitane iz Postman response size/body prikaza. gRPC vrednosti su ocitane u Postman gRPC okruzenju iz prikazane response poruke. Tacni zahtevi su dokumentovani u `docs/postman-response-size.md`.

## 3. k6 rezultati

| Protokol | Scenario | VU | Prosecna latencija | p95 latencija | Max latencija | RPS |
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

## 4. Velicina odgovora

| Protokol | Scenario | Velicina odgovora |
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

Napomena: Postman gRPC prikazuje dekodovanu poruku kroz JSON interfejs. Zbog toga su gRPC vrednosti u tabeli vrednosti response payload-a vidljivog u Postmanu, a ne Wireshark merenje sirovih HTTP/2 frame-ova.

## 5. Docker stats analiza

CPU i RAM potrosnja merena je komandom `docker stats` tokom svakog 500 VU k6 testa. Za svaki protokol i scenario sacuvan je poseban fajl u `tests/results/docker-stats-<protocol>-<scenario>-500vu.txt`.

Tabela prikazuje peak CPU i peak RAM aktivnog servisnog kontejnera i PostgreSQL kontejnera tokom istog testa. Svi ostali kontejneri su takodje mereni u raw fajlovima, ali nisu prikazani u tabeli jer su u tom konkretnom testu uglavnom idle.

| Protokol | Scenario | Aktivni servis CPU peak | Aktivni servis RAM peak | PostgreSQL CPU peak | PostgreSQL RAM peak |
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

CPU procenat moze biti veci od 100% zato sto Docker prikazuje zbir upotrebe vise CPU jezgara.

## 6. Analiza rezultata

U scenariju A REST postize najbolju propusnost pri 500 VU, oko 454 RPS, uz znatno nizu prosecnu latenciju od GraphQL i gRPC varijanti u istom merenju. GraphQL i gRPC imaju vece latencije pri ingestion opterecenju, sto se vidi i kroz veci CPU pritisak kod aktivnog servisnog kontejnera i PostgreSQL baze.

U scenariju B REST ima najbolju latenciju jer koristi namenski endpoint koji vraca samo trazena polja. GraphQL ostvaruje fleksibilnost kroz izbor polja, ali pri 500 VU pokazuje veci overhead zbog parsiranja GraphQL upita i resolver sloja. gRPC selective monitoring je takodje opteretio i servisni kontejner i bazu, ali je zadrzao bolju propusnost od GraphQL-a u ovom run-u.

U scenariju C dominantan faktor postaje baza podataka i agregacioni upiti. To se najjasnije vidi kod Docker stats rezultata: PostgreSQL CPU peak je 1183.36% za REST heavy querying i 1407.81% za gRPC heavy querying. U takvim uslovima latencija vise ne zavisi samo od formata poruke, vec od slozenosti SQL agregacija i konkurentnog pristupa bazi.

Kod velicine odgovora GraphQL daje najmanji payload kada se u query-ju trazi mali broj polja, sto se posebno vidi u Scenario C rezultatu. REST i GraphQL u Scenario B imaju slicnu velicinu jer oba vracaju po 100 zapisa sa dva ista polja.

Docker stats rezultati pokazuju da se procesorski trosak serijalizacije/deserijalizacije ne moze posmatrati izolovano od baze. REST ingestion najvise opterecuje REST servisni kontejner, dok heavy querying najvise opterecuje PostgreSQL. GraphQL servis ima stabilan CPU peak od oko 133-139% kroz sva tri scenarija, ali baza ostaje znacajno opterecena. gRPC pokazuje najvisi CPU i RAM pritisak na servisni kontejner u ingestion i selective scenarijima, sto ukazuje na skuplju obradu u ovoj konkretnoj implementaciji i pri ovom lokalnom Docker okruzenju.

## 7. Ogranicenja

Merenja su izvrsena lokalno u Docker Desktop okruzenju, pa zavise od resursa racunara, Docker ogranicenja i trenutnog opterecenja sistema. Docker stats meri potrosnju na nivou kontejnera i ne izoluje samo trosak serijalizacije i deserijalizacije.

Postman gRPC merenje je korisno za zahtev iz specifikacije, ali nije isto sto i niskonivojsko merenje binarnih HTTP/2 frame-ova. Za takvu analizu bio bi potreban Wireshark ili dodatna instrumentacija.

## 8. Zakljucak

REST je najjednostavniji za implementaciju, testiranje i debagovanje, a u selective scenariju pokazuje odlicne performanse kada je endpoint prilagodjen konkretnom slucaju. GraphQL je najfleksibilniji za klijente koji traze razlicite kombinacije polja, ali uvodi dodatni overhead pri vecem opterecenju. gRPC je pogodan za tipiziranu internu komunikaciju i ima dobre rezultate u selective monitoringu, dok heavy querying scenario pokazuje da performanse sistema mogu biti ogranicene bazom podataka.
