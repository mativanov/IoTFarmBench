CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sensor_id VARCHAR UNIQUE NOT NULL,
    farm_id VARCHAR,
    region VARCHAR,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS sensor_readings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(id) ON DELETE CASCADE,
    timestamp TIMESTAMPTZ NOT NULL,

    crop_type VARCHAR,
    soil_moisture_percent DOUBLE PRECISION,
    soil_ph DOUBLE PRECISION,
    temperature_c DOUBLE PRECISION,
    rainfall_mm DOUBLE PRECISION,
    humidity_percent DOUBLE PRECISION,
    sunlight_hours DOUBLE PRECISION,

    irrigation_type VARCHAR,
    fertilizer_type VARCHAR,
    pesticide_usage_ml DOUBLE PRECISION,

    sowing_date DATE,
    harvest_date DATE,
    total_days INTEGER,

    yield_kg_per_hectare DOUBLE PRECISION,
    ndvi_index DOUBLE PRECISION,

    crop_disease_status VARCHAR,

    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_sensor_readings_device_timestamp
    ON sensor_readings (device_id, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_sensor_readings_timestamp
    ON sensor_readings (timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_devices_sensor_id
    ON devices (sensor_id);

CREATE INDEX IF NOT EXISTS idx_devices_region
    ON devices (region);

CREATE INDEX IF NOT EXISTS idx_sensor_readings_crop_type
    ON sensor_readings (crop_type);
