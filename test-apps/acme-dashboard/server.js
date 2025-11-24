const express = require('express');
const path = require('path');
const { primusIdentityMiddleware } = require('primus-identity-validator');
const { createLogger, LogLevel } = require('@primus-saas/logging');

require('dotenv').config();

const app = express();

// Serve the Frontend (HTML/CSS)
app.use(express.static(path.join(__dirname, 'public')));
app.use(express.json());

// ==================================================================
// Logging (structured, request-enriched)
// ==================================================================
const logger = createLogger({
    applicationId: process.env.API_AUDIENCE || 'PSP-CLI-711224',
    environment: process.env.NODE_ENV || 'development',
    minLevel: LogLevel.INFO
});

/**
 * Lightweight request logger that mirrors primusLoggingMiddleware until dist exports catch up
 */
function requestLoggingMiddleware(loggerInstance) {
    return (req, res, next) => {
        const requestId = (req.headers['x-request-id']) ||
            `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

        const baseContext = {
            requestId,
            method: req.method,
            path: req.path,
            ip: req.ip || req.socket.remoteAddress
        };

        res.setHeader('X-Request-ID', requestId);

        const getUserContext = () => {
            const user = req.primusUser || req.user;
            return user ? {
                userId: user.userId || user.id || user.sub,
                userEmail: user.email,
                userRoles: user.roles
            } : {};
        };

        // Attach enriched logger to request
        req.logger = {
            debug: (message, context = {}) => loggerInstance.debug(message, { ...baseContext, ...getUserContext(), ...context }),
            info: (message, context = {}) => loggerInstance.info(message, { ...baseContext, ...getUserContext(), ...context }),
            warn: (message, context = {}) => loggerInstance.warn(message, { ...baseContext, ...getUserContext(), ...context }),
            error: (message, context = {}) => loggerInstance.error(message, { ...baseContext, ...getUserContext(), ...context }),
            critical: (message, context = {}) => loggerInstance.critical(message, { ...baseContext, ...getUserContext(), ...context }),
            startTimer: () => loggerInstance.startTimer()
        };

        // Maintain request reference for enrichers
        loggerInstance.setRequest(req);
        res.on('finish', () => {
            const duration = Date.now() - req.startTime;
            loggerInstance.info('Request completed', {
                ...baseContext,
                ...getUserContext(),
                statusCode: res.statusCode,
                duration
            });
            loggerInstance.clearRequest();
        });

        req.startTime = Date.now();
        next();
    };
}

app.use(requestLoggingMiddleware(logger));

// ==================================================================
// Identity configuration (multi-issuer)
// ==================================================================
const API_AUDIENCE = process.env.API_AUDIENCE || 'PSP-CLI-711224';

const PRIMUS_CONFIG = {
    issuers: [
        {
            name: 'AzureAD',
            type: 'oidc',
            issuer: process.env.AZURE_AD_ISSUER || 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            authority: process.env.AZURE_AD_AUTHORITY || 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            audiences: [
                process.env.AZURE_AD_AUDIENCE || 'acc675f1-e32f-40b9-a0c6-716066cc6890',
                API_AUDIENCE
            ]
        },
        {
            name: 'LocalAuth',
            type: 'jwt',
            issuer: process.env.LOCAL_ISSUER || 'http://localhost:4000',
            secret: process.env.LOCAL_SECRET || 'local-dev-secret-123',
            audiences: [API_AUDIENCE]
        }
    ],
    clockSkew: 300
};

const primusAuth = primusIdentityMiddleware(PRIMUS_CONFIG);

// Bridge identity -> logging so user context is captured in logs
function attachUserContext(req, _res, next) {
    if (req.primusUser) {
        req.user = {
            userId: req.primusUser.userId,
            email: req.primusUser.email,
            roles: req.primusUser.roles
        };
    }
    next();
}

// ==================================================================
// API ENDPOINTS
// ==================================================================

// Health check endpoint (no auth required)
app.get('/api/health', (_req, res) => {
    res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// Protected Data Endpoint
app.get('/api/revenue-stats', primusAuth, attachUserContext, (req, res) => {
    req.logger.info('Revenue stats requested');

    res.json({
        company: "Acme Corp",
        revenue: "$4,250,000",
        growth: "+125%",
        activeUsers: 14500,
        lastUpdated: new Date().toISOString(),
        user: req.primusUser
    });
});

// Export app for tests
module.exports = { app, logger };

// Start server when run directly
if (require.main === module) {
    const port = process.env.PORT || 3000;
    app.listen(port, () => {
        logger.info('Acme Dashboard started', { port });
        console.log('?? Acme Dashboard running at http://localhost:' + port);
        console.log('?? API Endpoints:');
        console.log('   - GET /api/revenue-stats (protected)');
        console.log('   - GET /api/health (public)');
    });
}
