import grpc from 'k6/net/grpc';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

const client = new grpc.Client();
client.load(['../../services/grpc-service/Protos'], 'farm_benchmark.proto');

const GRPC_TARGET = __ENV.GRPC_HOST || __ENV.GRPC_TARGET || 'localhost:5001';
const successfulRequests = new Counter('successful_requests');
const requestFailureRate = new Rate('request_failure_rate');

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURATION || '30s',
  thresholds: {
    grpc_req_duration: ['p(95)<1000'],
    checks: ['rate>0.95'],
    request_failure_rate: ['rate<0.05']
  },
  tags: {
    protocol: 'grpc',
    scenario: 'high-frequency-ingestion'
  }
};

const crops = ['Wheat', 'Corn', 'Rice', 'Soybean', 'Barley'];
const regions = ['North', 'South', 'East', 'West', 'Central'];

export default function () {
  client.connect(GRPC_TARGET, { plaintext: true });

  const payload = {
    sensorId: `k6-grpc-sensor-${__VU}-${__ITER}`,
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

  const response = client.invoke(
    'iotfarmbench.grpc.FarmBenchmarkService/CreateReading',
    payload
  );

  const ok = check(response, {
    'gRPC create reading status OK': (r) => r && r.status === grpc.StatusOK
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  client.close();
  sleep(1);
}

function randomBetween(min, max) {
  return Math.round((min + Math.random() * (max - min)) * 100) / 100;
}
