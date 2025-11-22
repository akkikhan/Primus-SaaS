const axios = require('axios');
const tokenUrl = 'http://localhost:3100/get-test-token';
const protectedUrl = 'http://localhost:3100/protected';
const CONCURRENCY = 50; // number of parallel requests
const TOTAL_REQUESTS = 200; // total requests to send

async function getToken() {
    const resp = await axios.get(tokenUrl);
    return resp.data.token;
}

async function fireRequests(token) {
    let completed = 0;
    const start = Date.now();
    const promises = [];
    for (let i = 0; i < TOTAL_REQUESTS; i++) {
        const p = axios.get(protectedUrl, { headers: { Authorization: `Bearer ${token}` } })
            .then(() => { completed++; })
            .catch(() => { completed++; });
        promises.push(p);
        // throttle concurrency
        if (promises.length >= CONCURRENCY) {
            await Promise.race(promises);
        }
    }
    await Promise.all(promises);
    const duration = (Date.now() - start) / 1000;
    console.log(`Load test completed: ${TOTAL_REQUESTS} requests in ${duration.toFixed(2)}s (${(TOTAL_REQUESTS / duration).toFixed(2)} req/s)`);
}

(async () => {
    try {
        const token = await getToken();
        await fireRequests(token);
    } catch (err) {
        console.error('Load test error:', err.message);
    }
})();
