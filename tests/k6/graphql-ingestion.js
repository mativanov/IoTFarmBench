import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5002';
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
    protocol: 'graphql',
    scenario: 'high-frequency-ingestion'
  }
};

const mutation = `
  mutation CreateReading($input: CreateSensorReadingInput!) {
    createReading(input: $input) {
      id
      deviceId
      timestamp
    }
  }
`;

const crops = ['Wheat', 'Corn', 'Rice', 'Soybean', 'Barley'];
const regions = ['North', 'South', 'East', 'West', 'Central'];

export default function () {
  const variables = {
    input: {
      sensorId: `k6-graphql-sensor-${__VU}-${__ITER}`,
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
    }
  };

  const response = http.post(`${BASE_URL}/`, JSON.stringify({ query: mutation, variables }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'createReading' }
  });

  const ok = check(response, {
    'mutation succeeded': (r) => r.status === 200 && !r.json('errors')
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  sleep(1);
}

function randomBetween(min, max) {
  return Math.round((min + Math.random() * (max - min)) * 100) / 100;
}
