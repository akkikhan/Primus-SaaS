const express = require('express');
const { primusIdentityMiddleware } = require('primus-identity-validator');
const path = require('path');

const app = express();

// Serve the Frontend (HTML/CSS)
app.use(express.static(path.join(__dirname, 'public')));
app.use(express.json());

console.log("🏦 ACME Financial Dashboard Server Starting...");

// ==================================================================
// 🔐 PRIMUS IDENTITY VALIDATOR CONFIGURATION (App ID Only Model)
// ==================================================================
// Load environment variables (optional - falls back to defaults)
require('dotenv').config();

// Primus App ID from portal - used as the audience claim in all tokens
const API_AUDIENCE = process.env.API_AUDIENCE || 'PSP-CLI-711224';

const PRIMUS_CONFIG = {
    issuers: [
        {
            name: 'AzureAD',
            type: 'oidc',
            issuer: process.env.AZURE_AD_ISSUER || 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            authority: process.env.AZURE_AD_AUTHORITY || 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            audiences: [
                'acc675f1-e32f-40b9-a0c6-716066cc6890',  // ✅ Azure AD client ID (actual audience in ID token)
                API_AUDIENCE  // Also accept Primus App ID for compatibility
            ]
        },
        {
            name: 'LocalAuth',
            type: 'jwt',
            issuer: process.env.LOCAL_ISSUER || 'http://localhost:4000',
            secret: process.env.LOCAL_SECRET || 'local-dev-secret-123',
            audiences: [API_AUDIENCE]  // ✅ Primus App ID (not Azure client ID)
        }
    ],
    clockSkew: 300
};

// Initialize Middleware
const primusAuth = primusIdentityMiddleware(PRIMUS_CONFIG);

console.log("✅ Primus Identity Validator configured");
console.log("   - Primus App ID (Audience):", API_AUDIENCE);
console.log("   - Issuers:", PRIMUS_CONFIG.issuers.map(i => i.name).join(', '));


// ==================================================================
// 📊 API ENDPOINTS
// ==================================================================

// Debug middleware to log token info
app.use('/api/revenue-stats', (req, res, next) => {
    console.log('\n🔍 DEBUG: Incoming request to /api/revenue-stats');
    const authHeader = req.headers.authorization;
    if (authHeader) {
        const token = authHeader.replace('Bearer ', '');
        console.log('📝 Token received (first 50 chars):', token.substring(0, 50) + '...');

        // Decode token to see claims (without verification)
        try {
            const parts = token.split('.');
            if (parts.length === 3) {
                const payload = JSON.parse(Buffer.from(parts[1], 'base64').toString());
                console.log('🎫 Token claims:', {
                    iss: payload.iss,
                    aud: payload.aud,
                    sub: payload.sub,
                    exp: payload.exp ? new Date(payload.exp * 1000).toISOString() : 'N/A'
                });
            }
        } catch (e) {
            console.log('⚠️ Could not decode token:', e.message);
        }
    } else {
        console.log('❌ No Authorization header found');
    }
    next();
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

// Protected Data Endpoint
// app.get('/api/revenue-stats', (req, res) => {
//     // This code only runs if the user is authenticated
//     res.json({
//         company: "Acme Corp",
//         revenue: "$4,250,000",
//         growth: "+125%",
//         activeUsers: 14500,
//         lastUpdated: new Date().toISOString(),
//         user: req.primusUser // The user who requested this
//     });
// });

// Health check endpoint (no auth required)
app.get('/api/health', (req, res) => {
    res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// Start Server
app.listen(3000, () => {
    console.log('🚀 Acme Dashboard running at http://localhost:3000');
    console.log('📝 API Endpoints:');
    console.log('   - GET /api/revenue-stats (protected)');
    console.log('   - GET /api/health (public)');
});
