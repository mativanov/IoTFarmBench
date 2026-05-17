import argparse
import os
from pathlib import Path
from typing import Any

import pandas as pd
import psycopg2
from psycopg2.extras import execute_values


EXPECTED_COLUMNS = [
    "farm_id",
    "region",
    "crop_type",
    "soil_moisture_%",
    "soil_pH",
    "temperature_C",
    "rainfall_mm",
    "humidity_%",
    "sunlight_hours",
    "irrigation_type",
    "fertilizer_type",
    "pesticide_usage_ml",
    "sowing_date",
    "harvest_date",
    "total_days",
    "yield_kg_per_hectare",
    "sensor_id",
    "timestamp",
    "latitude",
    "longitude",
    "NDVI_index",
    "crop_disease_status",
]

NUMERIC_COLUMNS = [
    "soil_moisture_%",
    "soil_pH",
    "temperature_C",
    "rainfall_mm",
    "humidity_%",
    "sunlight_hours",
    "pesticide_usage_ml",
    "total_days",
    "yield_kg_per_hectare",
    "latitude",
    "longitude",
    "NDVI_index",
]


def env(name: str, default: str) -> str:
    return os.environ.get(name, default)


def connect():
    return psycopg2.connect(
        host=env("DB_HOST", "localhost"),
        port=env("DB_PORT", "5432"),
        dbname=env("DB_NAME", "iotfarmbench"),
        user=env("DB_USER", "postgres"),
        password=env("DB_PASSWORD", "postgres"),
    )


def find_first_csv(data_dir: Path) -> Path:
    if not data_dir.exists():
        raise FileNotFoundError(f"Data directory does not exist: {data_dir}")

    csv_files = sorted(path for path in data_dir.iterdir() if path.is_file() and path.suffix.lower() == ".csv")
    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in {data_dir}")

    return csv_files[0]


def clean_value(value: Any) -> Any:
    if pd.isna(value):
        return None
    return value


def parse_date_columns(df: pd.DataFrame) -> pd.DataFrame:
    parsed = df.copy()
    parsed["timestamp"] = pd.to_datetime(parsed["timestamp"], errors="coerce", utc=True)
    parsed["sowing_date"] = pd.to_datetime(parsed["sowing_date"], errors="coerce").dt.date
    parsed["harvest_date"] = pd.to_datetime(parsed["harvest_date"], errors="coerce").dt.date
    return parsed


def parse_numeric_columns(df: pd.DataFrame) -> pd.DataFrame:
    parsed = df.copy()
    for column in NUMERIC_COLUMNS:
        parsed[column] = pd.to_numeric(parsed[column], errors="coerce")
    return parsed


def clean_int(value: Any) -> Any:
    if pd.isna(value):
        return None
    return int(value)


def validate_columns(df: pd.DataFrame) -> None:
    actual = list(df.columns)
    if actual != EXPECTED_COLUMNS:
        missing = [column for column in EXPECTED_COLUMNS if column not in actual]
        extra = [column for column in actual if column not in EXPECTED_COLUMNS]
        raise ValueError(
            "CSV columns do not match expected dataset schema. "
            f"Missing: {missing or 'none'}. Extra: {extra or 'none'}."
        )


def import_devices(cursor, df: pd.DataFrame) -> tuple[dict[str, str], int]:
    devices = (
        df[["sensor_id", "farm_id", "region", "latitude", "longitude"]]
        .dropna(subset=["sensor_id"])
        .drop_duplicates(subset=["sensor_id"])
    )

    rows = [
        tuple(clean_value(value) for value in row)
        for row in devices.itertuples(index=False, name=None)
    ]

    if rows:
        execute_values(
            cursor,
            """
            INSERT INTO devices (sensor_id, farm_id, region, latitude, longitude)
            VALUES %s
            ON CONFLICT (sensor_id) DO UPDATE SET
                farm_id = COALESCE(EXCLUDED.farm_id, devices.farm_id),
                region = COALESCE(EXCLUDED.region, devices.region),
                latitude = COALESCE(EXCLUDED.latitude, devices.latitude),
                longitude = COALESCE(EXCLUDED.longitude, devices.longitude)
            """,
            rows,
        )

    cursor.execute("SELECT sensor_id, id FROM devices")
    return {sensor_id: device_id for sensor_id, device_id in cursor.fetchall()}, len(rows)


def import_readings(cursor, df: pd.DataFrame, device_ids: dict[str, str]) -> tuple[int, int]:
    readings = []
    failed_rows = 0

    for _, row in df.iterrows():
        sensor_id = clean_value(row["sensor_id"])
        timestamp = clean_value(row["timestamp"])
        device_id = device_ids.get(sensor_id)

        if not sensor_id or device_id is None or timestamp is None or pd.isna(timestamp):
            failed_rows += 1
            continue

        readings.append(
            (
                device_id,
                timestamp.to_pydatetime(),
                clean_value(row["crop_type"]),
                clean_value(row["soil_moisture_%"]),
                clean_value(row["soil_pH"]),
                clean_value(row["temperature_C"]),
                clean_value(row["rainfall_mm"]),
                clean_value(row["humidity_%"]),
                clean_value(row["sunlight_hours"]),
                clean_value(row["irrigation_type"]),
                clean_value(row["fertilizer_type"]),
                clean_value(row["pesticide_usage_ml"]),
                clean_value(row["sowing_date"]),
                clean_value(row["harvest_date"]),
                clean_int(row["total_days"]),
                clean_value(row["yield_kg_per_hectare"]),
                clean_value(row["NDVI_index"]),
                clean_value(row["crop_disease_status"]),
            )
        )

    if readings:
        execute_values(
            cursor,
            """
            INSERT INTO sensor_readings (
                device_id,
                timestamp,
                crop_type,
                soil_moisture_percent,
                soil_ph,
                temperature_c,
                rainfall_mm,
                humidity_percent,
                sunlight_hours,
                irrigation_type,
                fertilizer_type,
                pesticide_usage_ml,
                sowing_date,
                harvest_date,
                total_days,
                yield_kg_per_hectare,
                ndvi_index,
                crop_disease_status
            )
            VALUES %s
            """,
            readings,
            page_size=1000,
        )

    return len(readings), failed_rows


def main() -> None:
    parser = argparse.ArgumentParser(description="Import Smart Farming IoT CSV data into PostgreSQL.")
    parser.add_argument(
        "csv_path",
        nargs="?",
        help="Optional path to the dataset CSV file. Defaults to the first CSV file in /app/data.",
    )
    args = parser.parse_args()

    csv_path = Path(args.csv_path) if args.csv_path else find_first_csv(Path("/app/data"))
    print(f"Using CSV file: {csv_path}")

    df = pd.read_csv(csv_path)
    validate_columns(df)
    df = parse_numeric_columns(df)
    df = parse_date_columns(df)

    with connect() as connection:
        with connection.cursor() as cursor:
            device_ids, imported_devices = import_devices(cursor, df)
            imported_readings, failed_rows = import_readings(cursor, df, device_ids)

    print(f"Imported devices count: {imported_devices}")
    print(f"Imported readings count: {imported_readings}")
    print(f"Failed rows count: {failed_rows}")


if __name__ == "__main__":
    main()
