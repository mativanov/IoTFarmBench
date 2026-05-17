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
    scenario: 'heavy-querying'
  }
};

const summaryQuery = `
  query {
    analyticsSummary {
      count
      avgTemperatureC
      avgHumidityPercent
      avgSoilMoisturePercent
      avgSoilPh
      avgRainfallMm
      avgSunlightHours
      avgNdviIndex
      avgYieldKgPerHectare
      minTimestamp
      maxTimestamp
    }
  }
`;

const byRegionQuery = `
  query {
    analyticsByRegion {
      region
      count
      avgTemperatureC
      avgHumidityPercent
      avgSoilMoisturePercent
      avgNdviIndex
      avgYieldKgPerHectare
    }
  }
`;

export default function () {
  const query = __ITER % 2 === 0 ? summaryQuery : byRegionQuery;
  const endpoint = __ITER % 2 === 0 ? 'analyticsSummary' : 'analyticsByRegion';

  const response = http.post(`${BASE_URL}/`, JSON.stringify({ query }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint }
  });

  const body = parseGraphQLResponse(response);
  const ok = check(response, {
    'analytics query succeeded': (r) =>
      r.status === 200 &&
      hasNoGraphQLErrors(body) &&
      hasExpectedAnalyticsData(body, endpoint)
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  sleep(1);
}

function parseGraphQLResponse(response) {
  try {
    return response.json();
  } catch {
    return null;
  }
}

function hasNoGraphQLErrors(body) {
  return Array.isArray(body?.errors) ? body.errors.length === 0 : body?.errors === undefined;
}

function hasExpectedAnalyticsData(body, endpoint) {
  if (endpoint === 'analyticsSummary') {
    return Number.isFinite(body?.data?.analyticsSummary?.count);
  }

  return Array.isArray(body?.data?.analyticsByRegion);
}
