import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 5,
      duration: '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
  },
};

const baseUrl = __ENV.BASE_URL;
if (!baseUrl) {
  throw new Error('BASE_URL is required, for example https://api.example.test');
}

export default function () {
  const response = http.get(`${baseUrl}/health/live`, {
    tags: { endpoint: 'health-live' },
  });

  check(response, {
    'health/live returns 200': (r) => r.status === 200,
  });
}
