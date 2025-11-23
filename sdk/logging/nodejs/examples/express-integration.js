const express = require('express');
const { createLogger, LogLevel } = require('../dist/index');

// Create Express app
const app = express();
app.use(express.json());

// Create logger
const logger = createLogger({
    applicationId: 'EXPRESS-DEMO-123',
    environment: 'development',
    minLevel: LogLevel.DEBUG
});

// Middleware to set request context
app.use((req, res, next) => {
    // Set request for context enrichment
    logger.setRequest(req);

    // Simulate Primus Identity Validator middleware
    // In real app, this would be done by @primus-saas/identity-validator
    req.primusUser = {
        userId: 'user-12345',
        email: 'john.doe@example.com',
        roles: ['user', 'admin']
    };

    req.primusTenantContext = {
        tenantId: 'acme-corp',
        tenantName: 'Acme Corporation'
    };

    next();

    // Clear request after response
    res.on('finish', () => {
        logger.clearRequest();
    });
});

// Routes
app.get('/api/users', (req, res) => {
    logger.info('Fetching users');

    res.json({
        users: [
            { id: 1, name: 'John Doe' },
            { id: 2, name: 'Jane Smith' }
        ]
    });
});

app.post('/api/orders', (req, res) => {
    const { items, total } = req.body;

    logger.info('Order created', {
        orderId: 'ORD-789',
        items: items.length,
        total
    });

    res.json({
        success: true,
        orderId: 'ORD-789'
    });
});

app.get('/api/error', (req, res) => {
    try {
        throw new Error('Something went wrong!');
    } catch (error) {
        logger.error('Request failed', {
            error: error.message,
            stack: error.stack
        });

        res.status(500).json({ error: 'Internal server error' });
    }
});

// Start server
const PORT = 3000;
app.listen(PORT, () => {
    logger.info('Server started', { port: PORT });
    console.log(`\n✅ Express server running on http://localhost:${PORT}`);
    console.log('\nTry these endpoints:');
    console.log(`  GET  http://localhost:${PORT}/api/users`);
    console.log(`  POST http://localhost:${PORT}/api/orders`);
    console.log(`       Body: { "items": [1, 2, 3], "total": 99.99 }`);
    console.log(`  GET  http://localhost:${PORT}/api/error`);
    console.log('\nNotice how logs automatically include:');
    console.log('  - requestId (auto-generated)');
    console.log('  - userId (from primusUser)');
    console.log('  - tenantId (from primusTenantContext)');
    console.log('  - applicationId, environment (from logger config)\n');
});
