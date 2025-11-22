const express = require('express');
const { primusIdentityMiddleware } = require('primus-identity-validator');
const path = require('path');

const app = express();

// Serve the Frontend (HTML/CSS)
app.use(express.static(path.join(__dirname, 'public')));
app.use(express.json());

console.log("🏦 ACME Financial Dashboard Server Starting...");

// ==================================================================
// 🔐 PRIMUS IDENTITY VALIDATOR CONFIGURATION
// ==================================================================
// Azure AD Configuration
// Using current SDK interface
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',

    // IMPORTANT: In Azure AD mode, clientId must be the Azure AD Client ID
    // because the SDK uses this for audience validation.
    clientId: 'acc675f1-e32f-40b9-a0c6-716066cc6890',

    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',

    // Azure AD configuration
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043',
    audience: 'acc675f1-e32f-40b9-a0c6-716066cc6890' // Redundant but good for clarity
};

// Initialize Middleware
const primusAuth = primusIdentityMiddleware(PRIMUS_CONFIG);

console.log("✅ Primus Identity Validator configured");
console.log("   - Mode: Azure AD");
console.log("   - Tenant ID:", PRIMUS_CONFIG.tenantId);
console.log("   - Audience:", PRIMUS_CONFIG.audience);
console.log("   - Tracking ID:", PRIMUS_CONFIG.clientId);

// ==================================================================
// 📊 API ENDPOINTS
// ==================================================================

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
