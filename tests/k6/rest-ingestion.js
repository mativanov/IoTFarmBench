import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const successfulRequests = new Counter('successful_requests');
const requestFailureRate = new Rate('request_failure_rate');

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURATION || '30s',
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.05'],
    request_failure_rate: ['rate<0.05']
  },
  tags: {
    protocol: 'rest',
    scenario: 'high-frequency-ingestion'
  }
};

const crops = ['Wheat', 'Corn', 'Rice', 'Soybean', 'Barley'];
const regions = ['North', 'South', 'East', 'West', 'Central'];

export default function () {
  const payload = {
    sensorId: `k6-rest-sensor-${__VU}-${__ITER}`,
    farmId: `k6-farm-${__VU}`,
    region: regions[(__VU + __ITER) % regions.length],
    latitude: 44.0 + Math.random(),
    longitude: 20.0 + Math.random(),
    timestamp: new Date().toISOString(),
    cropType: crops[__ITER % crops.length],
    temperatureC: randomBetween(16, 35),
    humidityPercent: randomBetween(40, 90),
    soilMoisturePercent: randomBetween(20, 60),
    soilPh: randomBetween(5.5, 7.8),
    rainfallMm: randomBetween(0, 20),
    sunlightHours: randomBetween(2, 12),
    ndviIndex: randomBetween(0.25, 0.9)
  };

  const response = http.post(`${BASE_URL}/api/readings`, JSON.stringify(payload), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'create-reading' }
  });

  const ok = check(response, {
    'created reading': (r) => r.status === 201
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  sleep(1);
}

function randomBetween(min, max) {
  return Math.round((min + Math.random() * (max - min)) * 100) / 100;
}
