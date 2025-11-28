import express, { Request, Response, NextFunction } from 'express';
import { PrimusIdentityValidator, PrimusIdentityOptions } from '@primus-saas/identity-validator';
import { Logger, LoggerOptions, primusLoggingMiddleware } from '@primus-saas/logging';

const app = express();
const PORT = process.env.PORT || 3000;

// ============================================================================
// LOGGING CONFIGURATION
// ============================================================================
const loggingOptions: PrimusLoggingOptions = {
    applicationName: 'NPM-Integration-Test',
    environment: 'Development',
    minimumLevel: 'Debug',
    enableConsole: true,
    enableFile: true,
    filePath: './logs',
    fileRotation: {
        enabled: true,
        maxFileSizeMB: 10,
        maxFiles: 5
    },
    piiMasking: {
        enabled: true,
        patterns: ['email', 'phone', 'ssn', 'creditCard']
    },
    structuredLogging: true,
    enrichers: {
        machineName: true,
        processId: true,
        threadId: true,
        timestamp: true
    }
};

// Initialize logger
const logger = new PrimusLogger(loggingOptions);

// Apply logging middleware
app.use(primusLoggingMiddleware(logger));

// ============================================================================
// IDENTITY VALIDATOR CONFIGURATION
// ============================================================================
const identityOptions: PrimusIdentityOptions = {
    issuers: [
        {
            name: 'AzureAD',
            type: 'AzureAD',
            authority: process.env.AZURE_AD_AUTHORITY || 'https://login.microsoftonline.com/common/v2.0',
            audience: process.env.AZURE_AD_AUDIENCE || 'api://default',
            validateIssuer: true,
            validateAudience: true,
            validateLifetime: true
        },
        {
            name: 'CustomJWT',
            type: 'JWT',
            issuer: 'https://primus-saas.com',
            audience: 'primus-api',
            secret: process.env.JWT_SECRET || 'your-secret-key-change-in-production',
            validateIssuer: true,
            validateAudience: true,
            validateLifetime: true
        }
    ],
    defaultIssuer: 'CustomJWT',
    enableCaching: true,
    cacheDuration: 300, // 5 minutes
    enableDiagnostics: true
};

// Initialize identity validator
const identityValidator = new PrimusIdentityValidator(identityOptions);

// ============================================================================
// MIDDLEWARE
// ============================================================================
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Request logging
app.use((req: Request, res: Response, next: NextFunction) => {
    logger.info(`Incoming request: ${req.method} ${req.path}`, {
        method: req.method,
        path: req.path,
        query: req.query,
        ip: req.ip
    });
    next();
});

// ============================================================================
// ROUTES - HEALTH & DIAGNOSTICS
// ============================================================================

app.get('/health', (req: Request, res: Response) => {
    logger.info('Health check requested');
    res.json({
        status: 'healthy',
        timestamp: new Date().toISOString(),
        service: 'npm-integration-test',
        modules: {
            identityValidator: 'active',
            logging: 'active'
        }
    });
});

app.get('/diagnostics', (req: Request, res: Response) => {
    logger.info('Diagnostics requested');

    const diagnostics = {
        timestamp: new Date().toISOString(),
        modules: {
            identityValidator: {
                version: '1.3.2',
                issuersConfigured: identityOptions.issuers.length,
                cachingEnabled: identityOptions.enableCaching,
                diagnosticsEnabled: identityOptions.enableDiagnostics
            },
            logging: {
                version: '1.2.1',
                applicationName: loggingOptions.applicationName,
                environment: loggingOptions.environment,
                minimumLevel: loggingOptions.minimumLevel,
                piiMaskingEnabled: loggingOptions.piiMasking?.enabled,
                fileLoggingEnabled: loggingOptions.enableFile,
                consoleLoggingEnabled: loggingOptions.enableConsole
            }
        },
        environment: {
            nodeVersion: process.version,
            platform: process.platform,
            uptime: process.uptime()
        }
    };

    res.json(diagnostics);
});

// ============================================================================
// ROUTES - LOGGING TESTS
// ============================================================================

app.post('/test/logging/levels', (req: Request, res: Response) => {
    const { level, message, metadata } = req.body;

    logger.info('Testing logging levels', { level, message });

    switch (level?.toLowerCase()) {
        case 'debug':
            logger.debug(message, metadata);
            break;
        case 'info':
            logger.info(message, metadata);
            break;
        case 'warning':
            logger.warning(message, metadata);
            break;
        case 'error':
            logger.error(message, metadata);
            break;
        case 'critical':
            logger.critical(message, metadata);
            break;
        default:
            logger.info(message, metadata);
    }

    res.json({
        success: true,
        level: level || 'info',
        message: 'Log entry created',
        timestamp: new Date().toISOString()
    });
});

app.post('/test/logging/pii-masking', (req: Request, res: Response) => {
    const testData = {
        email: 'test@example.com',
        phone: '+1-555-123-4567',
        ssn: '123-45-6789',
        creditCard: '4532-1234-5678-9010',
        normalData: 'This should not be masked'
    };

    logger.info('Testing PII masking', testData);

    res.json({
        success: true,
        message: 'PII masking test completed',
        testData: testData,
        note: 'Check logs to verify masking'
    });
});

app.post('/test/logging/structured', (req: Request, res: Response) => {
    const structuredData = {
        userId: 'user-123',
        action: 'test-action',
        resource: 'test-resource',
        metadata: {
            browser: 'Chrome',
            os: 'Windows',
            version: '1.0.0'
        },
        timestamp: new Date().toISOString()
    };

    logger.info('Structured logging test', structuredData);

    res.json({
        success: true,
        message: 'Structured logging test completed',
        data: structuredData
    });
});

// ============================================================================
// ROUTES - IDENTITY VALIDATOR TESTS
// ============================================================================

app.post('/test/identity/validate-token', async (req: Request, res: Response) => {
    try {
        const { token, issuerName } = req.body;

        if (!token) {
            logger.warning('Token validation attempted without token');
            return res.status(400).json({
                success: false,
                error: 'Token is required'
            });
        }

        logger.info('Validating token', { issuerName: issuerName || 'default' });

        const validationResult = await identityValidator.validateToken(token, issuerName);

        logger.info('Token validation successful', {
            issuer: validationResult.issuer,
            subject: validationResult.claims?.sub
        });

        res.json({
            success: true,
            message: 'Token validated successfully',
            result: {
                issuer: validationResult.issuer,
                claims: validationResult.claims,
                isValid: true
            }
        });
    } catch (error: any) {
        logger.error('Token validation failed', {
            error: error.message,
            stack: error.stack
        });

        res.status(401).json({
            success: false,
            error: error.message
        });
    }
});

app.get('/test/identity/protected',
    identityValidator.middleware(),
    (req: Request, res: Response) => {
        const user = (req as any).user;

        logger.info('Protected route accessed', {
            userId: user?.sub,
            email: user?.email
        });

        res.json({
            success: true,
            message: 'Protected route accessed successfully',
            user: user
        });
    }
);

app.post('/test/identity/multi-issuer', async (req: Request, res: Response) => {
    try {
        const { azureToken, customToken } = req.body;

        const results = {
            azure: null as any,
            custom: null as any
        };

        if (azureToken) {
            try {
                results.azure = await identityValidator.validateToken(azureToken, 'AzureAD');
                logger.info('Azure AD token validated');
            } catch (error: any) {
                results.azure = { error: error.message };
                logger.warning('Azure AD token validation failed', { error: error.message });
            }
        }

        if (customToken) {
            try {
                results.custom = await identityValidator.validateToken(customToken, 'CustomJWT');
                logger.info('Custom JWT token validated');
            } catch (error: any) {
                results.custom = { error: error.message };
                logger.warning('Custom JWT token validation failed', { error: error.message });
            }
        }

        res.json({
            success: true,
            message: 'Multi-issuer validation completed',
            results: results
        });
    } catch (error: any) {
        logger.error('Multi-issuer validation error', {
            error: error.message,
            stack: error.stack
        });

        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// ============================================================================
// ROUTES - INTEGRATION TESTS
// ============================================================================

app.post('/test/integration/full-flow',
    identityValidator.middleware({ optional: true }),
    async (req: Request, res: Response) => {
        const user = (req as any).user;
        const testId = `test-${Date.now()}`;

        logger.info('Full integration test started', {
            testId: testId,
            authenticated: !!user,
            userId: user?.sub
        });

        // Test logging with different levels
        logger.debug('Debug level test', { testId });
        logger.info('Info level test', { testId });
        logger.warning('Warning level test', { testId });

        // Test PII masking
        logger.info('PII test', {
            testId,
            email: 'sensitive@example.com',
            phone: '555-1234'
        });

        // Test structured logging
        logger.info('Structured test', {
            testId,
            metadata: {
                browser: req.headers['user-agent'],
                ip: req.ip,
                timestamp: new Date().toISOString()
            }
        });

        logger.info('Full integration test completed', { testId });

        res.json({
            success: true,
            testId: testId,
            message: 'Integration test completed successfully',
            results: {
                logging: 'passed',
                identity: user ? 'authenticated' : 'anonymous',
                piiMasking: 'tested',
                structuredLogging: 'tested'
            }
        });
    }
);

// ============================================================================
// ERROR HANDLING
// ============================================================================

app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
    logger.error('Unhandled error', {
        error: err.message,
        stack: err.stack,
        path: req.path,
        method: req.method
    });

    res.status(500).json({
        success: false,
        error: 'Internal server error',
        message: err.message
    });
});

// ============================================================================
// SERVER START
// ============================================================================

app.listen(PORT, () => {
    logger.info(`NPM Integration Test Server started`, {
        port: PORT,
        environment: loggingOptions.environment,
        modules: {
            identityValidator: 'initialized',
            logging: 'initialized'
        }
    });

    console.log(`
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║        Primus SaaS NPM Integration Test Server                ║
║                                                                ║
║  Server running on: http://localhost:${PORT}                     ║
║                                                                ║
║  Modules Loaded:                                               ║
║    ✓ @primus-saas/identity-validator (v1.3.2)                 ║
║    ✓ @primus-saas/logging (v1.2.1)                            ║
║                                                                ║
║  Available Endpoints:                                          ║
║    GET  /health                    - Health check              ║
║    GET  /diagnostics               - Module diagnostics        ║
║                                                                ║
║  Logging Tests:                                                ║
║    POST /test/logging/levels       - Test log levels          ║
║    POST /test/logging/pii-masking  - Test PII masking         ║
║    POST /test/logging/structured   - Test structured logs     ║
║                                                                ║
║  Identity Tests:                                               ║
║    POST /test/identity/validate-token  - Validate JWT         ║
║    GET  /test/identity/protected       - Protected route      ║
║    POST /test/identity/multi-issuer    - Multi-issuer test    ║
║                                                                ║
║  Integration Tests:                                            ║
║    POST /test/integration/full-flow    - Complete flow test   ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
    `);
});

export default app;
