const { spawn } = require('child_process');
const http = require('http');

console.log('🚀 Starting Express integration validation...\n');

// Start the Express server
const server = spawn('node', ['examples/express-integration.js'], {
    cwd: __dirname
});

let serverOutput = '';

server.stdout.on('data', (data) => {
    serverOutput += data.toString();
    process.stdout.write(data);
});

server.stderr.on('data', (data) => {
    process.stderr.write(data);
});

// Wait for server to start, then make requests
setTimeout(() => {
    console.log('\n📡 Making test requests...\n');

    // Request 1: GET /api/users
    http.get('http://localhost:3000/api/users', (res) => {
        let data = '';
        res.on('data', chunk => data += chunk);
        res.on('end', () => {
            console.log('\n✅ GET /api/users response:', data);

            // Request 2: POST /api/orders
            const postData = JSON.stringify({ items: [1, 2, 3], total: 99.99 });
            const options = {
                hostname: 'localhost',
                port: 3000,
                path: '/api/orders',
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Content-Length': postData.length
                }
            };

            const req = http.request(options, (res) => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', () => {
                    console.log('\n✅ POST /api/orders response:', data);

                    // Give logs time to flush
                    setTimeout(() => {
                        console.log('\n\n📊 Validation Summary:');
                        console.log('='.repeat(50));

                        // Check for context enrichment in logs
                        const hasRequestId = serverOutput.includes('requestId');
                        const hasUserId = serverOutput.includes('userId');
                        const hasTenantId = serverOutput.includes('tenantId');
                        const hasApplicationId = serverOutput.includes('applicationId');

                        console.log(`✅ Request ID enrichment: ${hasRequestId ? 'PASS' : 'FAIL'}`);
                        console.log(`✅ User ID enrichment: ${hasUserId ? 'PASS' : 'FAIL'}`);
                        console.log(`✅ Tenant ID enrichment: ${hasTenantId ? 'PASS' : 'FAIL'}`);
                        console.log(`✅ Application ID: ${hasApplicationId ? 'PASS' : 'FAIL'}`);
                        console.log('='.repeat(50));

                        // Stop server
                        server.kill();
                        process.exit(0);
                    }, 500);
                });
            });

            req.write(postData);
            req.end();
        });
    }).on('error', (err) => {
        console.error('❌ Request failed:', err.message);
        server.kill();
        process.exit(1);
    });
}, 2000);

// Timeout after 10 seconds
setTimeout(() => {
    console.error('\n❌ Validation timeout');
    server.kill();
    process.exit(1);
}, 10000);
