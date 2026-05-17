export function mapDevice(row) {
  return {
    id: row.id,
    sensorId: row.sensor_id,
    farmId: row.farm_id,
    region: row.region,
    latitude: toFloat(row.latitude),
    longitude: toFloat(row.longitude)
  };
}

export function mapReading(row) {
  return {
    id: row.id,
    deviceId: row.device_id,
    timestamp: toIsoString(row.timestamp),
    cropType: row.crop_type,
    soilMoisturePercent: toFloat(row.soil_moisture_percent),
    soilPh: toFloat(row.soil_ph),
    temperatureC: toFloat(row.temperature_c),
    rainfallMm: toFloat(row.rainfall_mm),
    humidityPercent: toFloat(row.humidity_percent),
    sunlightHours: toFloat(row.sunlight_hours),
    irrigationType: row.irrigation_type,
    fertilizerType: row.fertilizer_type,
    pesticideUsageMl: toFloat(row.pesticide_usage_ml),
    sowingDate: toDateString(row.sowing_date),
    harvestDate: toDateString(row.harvest_date),
    totalDays: row.total_days,
    yieldKgPerHectare: toFloat(row.yield_kg_per_hectare),
    ndviIndex: toFloat(row.ndvi_index),
    cropDiseaseStatus: row.crop_disease_status
  };
}

export function mapAnalyticsSummary(row) {
  return {
    count: Number(row.count ?? 0),
    avgTemperatureC: toFloat(row.avg_temperature_c),
    avgHumidityPercent: toFloat(row.avg_humidity_percent),
    avgSoilMoisturePercent: toFloat(row.avg_soil_moisture_percent),
    avgSoilPh: toFloat(row.avg_soil_ph),
    avgRainfallMm: toFloat(row.avg_rainfall_mm),
    avgSunlightHours: toFloat(row.avg_sunlight_hours),
    avgNdviIndex: toFloat(row.avg_ndvi_index),
    avgYieldKgPerHectare: toFloat(row.avg_yield_kg_per_hectare),
    minTimestamp: toIsoString(row.min_timestamp),
    maxTimestamp: toIsoString(row.max_timestamp)
  };
}

export function mapAnalyticsByRegion(row) {
  return {
    region: row.region,
    count: Number(row.count ?? 0),
    avgTemperatureC: toFloat(row.avg_temperature_c),
    avgHumidityPercent: toFloat(row.avg_humidity_percent),
    avgSoilMoisturePercent: toFloat(row.avg_soil_moisture_percent),
    avgNdviIndex: toFloat(row.avg_ndvi_index),
    avgYieldKgPerHectare: toFloat(row.avg_yield_kg_per_hectare)
  };
}

export function toDbTimestamp(value) {
  return value ? value.toISOString() : null;
}

export function emptyToNull(value) {
  return typeof value === 'string' && value.trim() === '' ? null : value ?? null;
}

function toFloat(value) {
  return value === null || value === undefined ? null : Number(value);
}

function toIsoString(value) {
  if (!value) {
    return null;
  }

  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}

function toDateString(value) {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    return value.toISOString().slice(0, 10);
  }

  return String(value).slice(0, 10);
}
