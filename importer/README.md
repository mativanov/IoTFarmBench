# CSV Importer

The importer loads the Smart Farming CSV dataset into PostgreSQL.

It performs two steps:

1. Upsert unique devices by `sensor_id`.
2. Insert sensor readings linked to the imported devices.

## Run

From the project root, after `docker compose up -d` has started PostgreSQL:

```powershell
docker compose run --rm importer
```

The container automatically imports the first `.csv` file found in `/app/data`, which is mounted from the project `data/` folder.

Optional environment variables:

- `DB_HOST`
- `DB_PORT`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
