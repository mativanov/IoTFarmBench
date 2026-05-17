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
    scenario: 'selective-monitoring'
  }
};

const query = `
  query {
    readings(limit: 100) {
      temperatureC
      humidityPercent
    }
  }
`;

export default function () {
  const response = http.post(`${BASE_URL}/`, JSON.stringify({ query }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'readings-selective' }
  });

  const ok = check(response, {
    'selective query succeeded': (r) => r.status === 200 && !r.json('errors')
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  sleep(1);
}
