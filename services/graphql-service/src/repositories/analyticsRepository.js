import { pool } from '../db.js';
import { emptyToNull, mapAnalyticsByRegion, mapAnalyticsSummary, toDbTimestamp } from './mapping.js';

export async function getAnalyticsSummary({ from, to, region, cropType }) {
  const result = await pool.query(
    `
    SELECT
      COUNT(*) AS count,
      AVG(sr.temperature_c) AS avg_temperature_c,
      AVG(sr.humidity_percent) AS avg_humidity_percent,
      AVG(sr.soil_moisture_percent) AS avg_soil_moisture_percent,
      AVG(sr.soil_ph) AS avg_soil_ph,
      AVG(sr.rainfall_mm) AS avg_rainfall_mm,
      AVG(sr.sunlight_hours) AS avg_sunlight_hours,
      AVG(sr.ndvi_index) AS avg_ndvi_index,
      AVG(sr.yield_kg_per_hectare) AS avg_yield_kg_per_hectare,
      MIN(sr.timestamp) AS min_timestamp,
      MAX(sr.timestamp) AS max_timestamp
    FROM sensor_readings sr
    JOIN devices d ON d.id = sr.device_id
    WHERE ($1::timestamptz IS NULL OR sr.timestamp >= $1)
      AND ($2::timestamptz IS NULL OR sr.timestamp <= $2)
      AND ($3::text IS NULL OR d.region = $3)
      AND ($4::text IS NULL OR sr.crop_type = $4)
    `,
    [toDbTimestamp(from), toDbTimestamp(to), emptyToNull(region), emptyToNull(cropType)]
  );

  return mapAnalyticsSummary(result.rows[0]);
}

export async function getAnalyticsByRegion() {
  const result = await pool.query(`
    SELECT
      d.region,
      COUNT(*) AS count,
      AVG(sr.temperature_c) AS avg_temperature_c,
      AVG(sr.humidity_percent) AS avg_humidity_percent,
      AVG(sr.soil_moisture_percent) AS avg_soil_moisture_percent,
      AVG(sr.ndvi_index) AS avg_ndvi_index,
      AVG(sr.yield_kg_per_hectare) AS avg_yield_kg_per_hectare
    FROM sensor_readings sr
    JOIN devices d ON d.id = sr.device_id
    GROUP BY d.region
    ORDER BY d.region NULLS LAST
  `);

  return result.rows.map(mapAnalyticsByRegion);
}
