import { pool } from '../db.js';
import { emptyToNull, mapDevice } from './mapping.js';

export async function getDevices() {
  const result = await pool.query(`
    SELECT id, sensor_id, farm_id, region, latitude, longitude
    FROM devices
    ORDER BY sensor_id
  `);

  return result.rows.map(mapDevice);
}

export async function getDeviceById(id) {
  const result = await pool.query(
    `
    SELECT id, sensor_id, farm_id, region, latitude, longitude
    FROM devices
    WHERE id = $1
    `,
    [id]
  );

  return result.rows[0] ? mapDevice(result.rows[0]) : null;
}

export async function getOrCreateDeviceId(client, input) {
  const result = await client.query(
    `
    INSERT INTO devices (sensor_id, farm_id, region, latitude, longitude)
    VALUES ($1, $2, $3, $4, $5)
    ON CONFLICT (sensor_id) DO UPDATE SET
      farm_id = COALESCE(EXCLUDED.farm_id, devices.farm_id),
      region = COALESCE(EXCLUDED.region, devices.region),
      latitude = COALESCE(EXCLUDED.latitude, devices.latitude),
      longitude = COALESCE(EXCLUDED.longitude, devices.longitude)
    RETURNING id
    `,
    [
      input.sensorId,
      emptyToNull(input.farmId),
      emptyToNull(input.region),
      input.latitude ?? null,
      input.longitude ?? null
    ]
  );

  return result.rows[0].id;
}
