const express = require('express');
const { primusIdentityMiddleware } = require('primus-identity-validator');
const fs = require('fs');

// Load credentials from the E2E test
const credentials = JSON.parse(fs.readFileSync('./e2e-credentials.json', 'utf8'));

console.log('='.repeat(60));
console.log('🚀 DEMO APPLICATION - Using Primus SDK');
console.log('='.repeat(60));
console.log('');
console.log('📋 Configuration:');
console.log(`   Client ID: ${credentials.clientId}`);
console.log(`   Secret: ${credentials.clientSecret.substring(0, 15)}...`);
console.log(`   Stack: ${credentials.stack}`);
console.log('');

const app = express();
const PORT = 3100;

// Initialize Primus Identity Validator middleware
const primusAuth = primusIdentityMiddleware({
    portalUrl: 'http://localhost:5267',
    clientId: credentials.clientId,
    clientSecret: credentials.clientSecret,
    jwtSecret: credentials.clientSecret, // Same as clientSecret for Local mode
    mode: 'Local'
});

// Public endpoint - no authentication
app.get('/', (req, res) => {
    res.json({
        message: 'Welcome to the Demo App!',
        endpoints: {
            public: 'GETl /',
            protected: 'GET /protected (requires Bearer token)',
            testToken: 'GET /get-test-token'
        }
    });
});

// Generate a test token for demonstration
app.get('/get-test-token', (req, res) => {
    const crypto = require('crypto');

    // Create a test payload
    const payload = {
        email: 'test@example.com',
        userId: '12345',
        role: 'user'
    };

    // HMAC signature: primusClientId|email|userId|role
    const message = `${credentials.clientId}|${payload.email}|${payload.userId}|${payload.role}`;
    const signature = crypto
        .createHmac('sha256', credentials.clientSecret)
        .update(message)
        .digest('base64');

    // Create the token
    const token = Buffer.from(JSON.stringify({
        primusClientId: credentials.clientId,
        email: payload.email,
        userId: payload.userId,
        role: payload.role,
        signature: signature
    })).toString('base64');

    res.json({
        message: 'Test token generated successfully!',
        instructions: 'Use this token in the Authorization header as "Bearer <token>"',
        token: token,
        exampleCurl: `curl -H "Authorization: Bearer ${token}" http://localhost:${PORT}/protected`,
        testIt: {
            method: 'GET',
            url: `http://localhost:${PORT}/protected`,
            headers: {
                'Authorization': `Bearer ${token}`
            }
        }
    });
});

// Protected endpoint - requires authentication
app.get('/protected', primusAuth, (req, res) => {
    res.json({
        message: '🎉 SUCCESS! You have accessed a protected resource!',
        authenticatedUser: req.primusUser,
        timestamp: new Date().toISOString()
    });
});

// Error handler
app.use((err, req, res, next) => {
    console.error('Error:', err.message);
    res.status(err.status || 500).json({
        error: err.message || 'Internal server error'
    });
});

// Start the server
app.listen(PORT, () => {
    console.log('='.repeat(60));
    console.log(`✅ Demo Application started successfully!`);
    console.log('='.repeat(60));
    console.log('');
    console.log(`🌐 Server running at: http://localhost:${PORT}`);
    console.log('');
    console.log('📖 Try these endpoints:');
    console.log(`   1. Public:      http://localhost:${PORT}/`);
    console.log(`   2. Get Token:   http://localhost:${PORT}/get-test-token`);
    console.log(`   3. Protected:   http://localhost:${PORT}/protected (requires token)`);
    console.log('');
    console.log('💡 Quick Test:');
    console.log('   1. Visit /get-test-token to generate a token');
    console.log('   2. Copy the token from the response');
    console.log('   3. Use it to access /protected');
    console.log('');
});
