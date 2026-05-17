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
    scenario: 'heavy-querying'
  }
};

export default function () {
  client.connect(GRPC_TARGET, { plaintext: true });

  const method = __ITER % 2 === 0
    ? 'iotfarmbench.grpc.FarmBenchmarkService/GetAnalyticsSummary'
    : 'iotfarmbench.grpc.FarmBenchmarkService/GetAnalyticsByRegion';

  const response = client.invoke(method, {});

  const ok = check(response, {
    'gRPC analytics status OK': (r) => r && r.status === grpc.StatusOK
  });
  successfulRequests.add(ok ? 1 : 0);
  requestFailureRate.add(!ok);

  client.close();
  sleep(1);
}
