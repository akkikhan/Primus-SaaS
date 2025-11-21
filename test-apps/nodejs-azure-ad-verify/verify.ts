import express from 'express';
import { createIdentityValidator } from 'primus-identity-validator';
import dotenv from 'dotenv';

dotenv.config();

const app = express();
const PORT = 3002;

// Initialize validator with Azure AD configuration
const auth = createIdentityValidator({
    primusDevClientId: 'TEST-CLIENT-ID',
    mode: 'AzureAd',
    azureAd: {
        tenantId: process.env.AZURE_TENANT_ID!,
        clientId: process.env.AZURE_CLIENT_ID!,
        audience: process.env.AZURE_AUDIENCE!,
    }
});

// Protected endpoint
app.get('/verify', auth.requireAuth, (req, res) => {
    res.json({
        status: 'success',
        message: 'Token is valid!',
        user: req.primusUser
    });
});

// Start server
const server = app.listen(PORT, () => {
    console.log(`Verification server running on port ${PORT}`);
    console.log('Waiting for request with Bearer token...');
});

// Handle graceful shutdown
process.on('SIGINT', () => {
    server.close(() => {
        console.log('Server stopped');
        process.exit(0);
    });
});
