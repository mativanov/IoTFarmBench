# Dataset Folder

Place the Smart Farming IoT CSV dataset in this folder.

The CSV columns must be exactly:

```text
farm_id
region
crop_type
soil_moisture_%
soil_pH
temperature_C
rainfall_mm
humidity_%
sunlight_hours
irrigation_type
fertilizer_type
pesticide_usage_ml
sowing_date
harvest_date
total_days
yield_kg_per_hectare
sensor_id
timestamp
latitude
longitude
NDVI_index
crop_disease_status
```

Example importer command from the project root:

```powershell
$env:DB_HOST="localhost"
python importer/import_dataset.py data/smart_farming_dataset.csv
```
