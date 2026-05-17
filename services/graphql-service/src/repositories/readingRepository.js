import { pool } from '../db.js';
import { getOrCreateDeviceId } from './deviceRepository.js';
import { emptyToNull, mapReading, toDbTimestamp } from './mapping.js';

const READING_COLUMNS = `
  id, device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
  temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
  irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
  harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
  crop_disease_status
`;

export async function getReadings({ deviceId, from, to, limit }) {
  const result = await pool.query(
    `
    SELECT ${READING_COLUMNS}
    FROM sensor_readings
    WHERE ($1::uuid IS NULL OR device_id = $1)
      AND ($2::timestamptz IS NULL OR timestamp >= $2)
      AND ($3::timestamptz IS NULL OR timestamp <= $3)
    ORDER BY timestamp DESC
    LIMIT $4
    `,
    [deviceId, toDbTimestamp(from), toDbTimestamp(to), limit]
  );

  return result.rows.map(mapReading);
}

export async function getReadingById(id) {
  const result = await pool.query(
    `
    SELECT ${READING_COLUMNS}
    FROM sensor_readings
    WHERE id = $1
    `,
    [id]
  );

  return result.rows[0] ? mapReading(result.rows[0]) : null;
}

export async function createReading(input) {
  const client = await pool.connect();

  try {
    await client.query('BEGIN');

    const deviceId = await getOrCreateDeviceId(client, input);
    const result = await client.query(
      `
      INSERT INTO sensor_readings (
        device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
        temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
        irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
        harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
        crop_disease_status
      )
      VALUES (
        $1, $2, $3, $4, $5,
        $6, $7, $8, $9,
        $10, $11, $12, $13,
        $14, $15, $16, $17,
        $18
      )
      RETURNING ${READING_COLUMNS}
      `,
      [
        deviceId,
        toDbTimestamp(input.timestamp),
        emptyToNull(input.cropType),
        input.soilMoisturePercent ?? null,
        input.soilPh ?? null,
        input.temperatureC ?? null,
        input.rainfallMm ?? null,
        input.humidityPercent ?? null,
        input.sunlightHours ?? null,
        emptyToNull(input.irrigationType),
        emptyToNull(input.fertilizerType),
        input.pesticideUsageMl ?? null,
        input.sowingDate ?? null,
        input.harvestDate ?? null,
        input.totalDays ?? null,
        input.yieldKgPerHectare ?? null,
        input.ndviIndex ?? null,
        emptyToNull(input.cropDiseaseStatus)
      ]
    );

    await client.query('COMMIT');
    return mapReading(result.rows[0]);
  } catch (error) {
    await client.query('ROLLBACK');
    throw error;
  } finally {
    client.release();
  }
}
