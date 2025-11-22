const express = require('express');
const { primusIdentityMiddleware } = require('primus-identity-validator');
const path = require('path');

const app = express();

// Serve the Frontend (HTML/CSS)
app.use(express.static(path.join(__dirname, 'public')));
app.use(express.json());

console.log("🏦 ACME Financial Dashboard Server Starting...");

// ==================================================================
// 🔐 SECURITY CONFIGURATION
// ==================================================================
// TODO: Paste credentials from Primus Portal here
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',
    tenantId: 'common'
};

// Initialize Middleware (Safe initialization for demo purposes)
const primusAuth = (req, res, next) => {
    if (PRIMUS_CONFIG.clientId === 'REPLACE_WITH_CLIENT_ID') {
        return res.status(500).json({ error: 'Security Module Not Configured' });
    }
    return primusIdentityMiddleware(PRIMUS_CONFIG)(req, res, next);
};

// ==================================================================
// 📊 API ENDPOINTS
// ==================================================================

// Proxy Login to avoid CORS issues and ensure Token Compatibility
const axios = require('axios');
const jwt = require('jsonwebtoken');

app.post('/login-proxy', async (req, res) => {
    try {
        // 1. Verify Credentials with Real Backend
        const response = await axios.post(`${PRIMUS_CONFIG.portalUrl}/api/auth/login`, req.body);
        const realUser = response.data;

        // 2. Generate a Token compatible with this App (Signed with our Client Secret)
        // This simulates an OAuth flow where the IDP issues a token FOR this app.
        const appToken = jwt.sign({
            sub: realUser.id || '12345',
            email: realUser.email,
            name: 'Admin User',
            role: realUser.role,
            iss: PRIMUS_CONFIG.portalUrl,
            aud: PRIMUS_CONFIG.clientId
        }, PRIMUS_CONFIG.jwtSecret, { expiresIn: '1h' });

        // 3. Return the compatible token
        res.json({
            token: appToken,
            email: realUser.email,
            role: realUser.role
        });

    } catch (error) {
        console.error('Login Proxy Error:', error.message);
        res.status(error.response?.status || 500).json(error.response?.data || { message: 'Login failed' });
    }
});

// Protected Data Endpoint
app.get('/api/revenue-stats', primusAuth, (req, res) => {
    // This code only runs if the user is authenticated
    res.json({
        company: "Acme Corp",
        revenue: "$4,250,000",
        growth: "+125%",
        activeUsers: 14500,
        lastUpdated: new Date().toISOString(),
        user: req.primusUser // The user who requested this
    });
});

// Start Server
app.listen(3000, () => {
    console.log('🚀 Acme Dashboard running at http://localhost:3000');
});
