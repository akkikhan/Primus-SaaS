# 🏋️ Load & Performance Testing Plan

**Tool:** K6 (Grafana K6)  
**Target:** `LiveDemoApi` (Release Build)

---

## 1. 🎯 Performance Goals

*   **Identity Middleware Overhead:** < 5ms per request.
*   **Logging Overhead:** < 2ms per log entry.
*   **Notification Throughput:** > 100 emails/sec (queued).

---

## 2. 📜 K6 Script Example

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 50, // 50 Virtual Users
  duration: '30s',
};

export default function () {
  // 1. Test Identity Overhead
  let res = http.get('http://localhost:5000/api/identity/whoami', {
    headers: { 'Authorization': 'Bearer <valid_test_token>' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'duration < 100ms': (r) => r.timings.duration < 100,
  });

  sleep(1);
}
```

---

## 3. 📉 Bottleneck Analysis

If performance degrades:

1.  **Identity:** Check if JWKS (Key Set) caching is working. It should not fetch keys from Authority on every request.
2.  **Logging:** Ensure `Console` logging is **disabled** in high-load tests (Console I/O is slow). Use file or async sinks.
3.  **Notifications:** Ensure SMTP calls are not blocking the main thread.

---

## 4. 🗓️ Schedule

*   **Baseline:** Run before every major version release (1.0 -> 2.0).
*   **Regression:** Run on every PR that touches `Middleware` or `Hot Paths`.
