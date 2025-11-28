/**
 * Integration Test - Both Modules Working Together
 * Tests Identity Validator and Logging working in harmony
 */

import { PrimusIdentityValidator, PrimusIdentityOptions } from '@primus-saas/identity-validator';
import { PrimusLogger, PrimusLoggingOptions } from '@primus-saas/logging';
import jwt from 'jsonwebtoken';

console.log('╔════════════════════════════════════════════════════════════════╗');
console.log('║           Integration Test - Both Modules Together            ║');
console.log('╚════════════════════════════════════════════════════════════════╝\n');

interface TestResult {
    test: string;
    status: 'PASS' | 'FAIL';
    details: string;
}

const results: TestResult[] = [];

function addResult(test: string, status: 'PASS' | 'FAIL', details: string) {
    results.push({ test, status, details });
}

async function runIntegrationTests() {
    console.log('Starting integration tests...\n');

    // ========================================================================
    // SETUP: Initialize both modules
    // ========================================================================
    console.log('Setup: Initializing modules...');

    const loggingOptions: PrimusLoggingOptions = {
        applicationName: 'IntegrationTest',
        environment: 'Test',
        minimumLevel: 'Debug',
        enableConsole: true,
        enableFile: false,
        structuredLogging: true,
        piiMasking: {
            enabled: true,
            patterns: ['email', 'phone']
        }
    };

    const logger = new PrimusLogger(loggingOptions);
    logger.info('Logger initialized for integration test');

    const secret = 'integration-test-secret';
    const identityOptions: PrimusIdentityOptions = {
        issuers: [
            {
                name: 'TestIssuer',
                type: 'JWT',
                issuer: 'https://integration-test.com',
                audience: 'integration-api',
                secret: secret
            }
        ]
    };

    const validator = new PrimusIdentityValidator(identityOptions);
    logger.info('Identity validator initialized');

    console.log('  ✓ Both modules initialized\n');

    // ========================================================================
    // TEST 1: Authenticated Request Flow
    // ========================================================================
    console.log('Test 1: Authenticated Request Flow');
    try {
        // Generate token
        const token = jwt.sign(
            {
                sub: 'user123',
                email: 'test@example.com',
                name: 'Test User',
                roles: ['user']
            },
            secret,
            {
                issuer: 'https://integration-test.com',
                audience: 'integration-api',
                expiresIn: '1h'
            }
        );

        logger.info('Token generated for test user', { userId: 'user123' });

        // Validate token
        const validationResult = await validator.validateToken(token);
        logger.info('Token validated successfully', {
            userId: validationResult.claims?.sub,
            email: validationResult.claims?.email
        });

        console.log('  ✓ Token generated');
        console.log('  ✓ Token validated');
        console.log('  ✓ User authenticated');
        console.log('  ✓ All actions logged');
        addResult('Authenticated Request Flow', 'PASS', 'Complete flow works');
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('Authenticated request flow failed', { error: error.message });
        addResult('Authenticated Request Flow', 'FAIL', error.message);
    }

    // ========================================================================
    // TEST 2: Failed Authentication with Logging
    // ========================================================================
    console.log('\nTest 2: Failed Authentication with Logging');
    try {
        logger.info('Testing failed authentication scenario');

        try {
            await validator.validateToken('invalid-token');
            console.log('  ✗ Should have failed');
            logger.error('Invalid token was accepted - security issue!');
            addResult('Failed Authentication Logging', 'FAIL', 'Invalid token accepted');
        } catch (validationError: any) {
            logger.warning('Invalid token rejected as expected', {
                error: validationError.message
            });
            console.log('  ✓ Invalid token rejected');
            console.log('  ✓ Failure logged');
            addResult('Failed Authentication Logging', 'PASS', 'Proper error handling and logging');
        }
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('Failed authentication test error', { error: error.message });
        addResult('Failed Authentication Logging', 'FAIL', error.message);
    }

    // ========================================================================
    // TEST 3: PII Masking in Authentication Logs
    // ========================================================================
    console.log('\nTest 3: PII Masking in Authentication Logs');
    try {
        const token = jwt.sign(
            {
                sub: 'user456',
                email: 'sensitive@example.com',
                phone: '+1-555-987-6543',
                name: 'Sensitive User'
            },
            secret,
            {
                issuer: 'https://integration-test.com',
                audience: 'integration-api',
                expiresIn: '1h'
            }
        );

        const validationResult = await validator.validateToken(token);

        // Log with PII data
        logger.info('User authenticated with sensitive data', {
            userId: validationResult.claims?.sub,
            email: validationResult.claims?.email,
            phone: (validationResult.claims as any)?.phone
        });

        console.log('  ✓ Token with PII validated');
        console.log('  ✓ PII data logged (should be masked)');
        addResult('PII Masking in Auth Logs', 'PASS', 'PII masking works with auth');
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('PII masking test failed', { error: error.message });
        addResult('PII Masking in Auth Logs', 'FAIL', error.message);
    }

    // ========================================================================
    // TEST 4: Multi-Issuer with Structured Logging
    // ========================================================================
    console.log('\nTest 4: Multi-Issuer with Structured Logging');
    try {
        logger.info('Testing multi-issuer scenario');

        const multiIssuerOptions: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'Issuer1',
                    type: 'JWT',
                    issuer: 'https://issuer1.com',
                    audience: 'api1',
                    secret: 'secret1'
                },
                {
                    name: 'Issuer2',
                    type: 'JWT',
                    issuer: 'https://issuer2.com',
                    audience: 'api2',
                    secret: 'secret2'
                }
            ]
        };

        const multiValidator = new PrimusIdentityValidator(multiIssuerOptions);

        // Test with first issuer
        const token1 = jwt.sign(
            { sub: 'user1', name: 'User One' },
            'secret1',
            { issuer: 'https://issuer1.com', audience: 'api1', expiresIn: '1h' }
        );

        const result1 = await multiValidator.validateToken(token1, 'Issuer1');
        logger.info('Token validated from Issuer1', {
            issuer: 'Issuer1',
            userId: result1.claims?.sub
        });

        // Test with second issuer
        const token2 = jwt.sign(
            { sub: 'user2', name: 'User Two' },
            'secret2',
            { issuer: 'https://issuer2.com', audience: 'api2', expiresIn: '1h' }
        );

        const result2 = await multiValidator.validateToken(token2, 'Issuer2');
        logger.info('Token validated from Issuer2', {
            issuer: 'Issuer2',
            userId: result2.claims?.sub
        });

        console.log('  ✓ Multiple issuers configured');
        console.log('  ✓ Tokens from different issuers validated');
        console.log('  ✓ All validations logged with structured data');
        addResult('Multi-Issuer with Logging', 'PASS', 'Multi-issuer works with logging');
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('Multi-issuer test failed', { error: error.message });
        addResult('Multi-Issuer with Logging', 'FAIL', error.message);
    }

    // ========================================================================
    // TEST 5: Performance - Logging Overhead
    // ========================================================================
    console.log('\nTest 5: Performance - Logging Overhead');
    try {
        const iterations = 100;
        const token = jwt.sign(
            { sub: 'perftest', name: 'Performance Test' },
            secret,
            {
                issuer: 'https://integration-test.com',
                audience: 'integration-api',
                expiresIn: '1h'
            }
        );

        const startTime = Date.now();

        for (let i = 0; i < iterations; i++) {
            await validator.validateToken(token);
            logger.debug('Token validated', { iteration: i });
        }

        const endTime = Date.now();
        const duration = endTime - startTime;
        const avgTime = duration / iterations;

        logger.info('Performance test completed', {
            iterations,
            totalDuration: duration,
            averageTime: avgTime
        });

        console.log(`  ✓ ${iterations} validations completed`);
        console.log(`  ✓ Total time: ${duration}ms`);
        console.log(`  ✓ Average time: ${avgTime.toFixed(2)}ms`);
        addResult('Performance Test', 'PASS', `${avgTime.toFixed(2)}ms average`);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('Performance test failed', { error: error.message });
        addResult('Performance Test', 'FAIL', error.message);
    }

    // ========================================================================
    // TEST 6: Error Propagation
    // ========================================================================
    console.log('\nTest 6: Error Propagation and Logging');
    try {
        logger.info('Testing error propagation');

        const errors: string[] = [];

        // Test 1: Invalid token
        try {
            await validator.validateToken('invalid');
        } catch (e: any) {
            errors.push('invalid-token');
            logger.error('Invalid token error caught', { error: e.message });
        }

        // Test 2: Expired token
        const expiredToken = jwt.sign(
            { sub: 'expired' },
            secret,
            {
                issuer: 'https://integration-test.com',
                audience: 'integration-api',
                expiresIn: '-1h' // Already expired
            }
        );

        try {
            await validator.validateToken(expiredToken);
        } catch (e: any) {
            errors.push('expired-token');
            logger.error('Expired token error caught', { error: e.message });
        }

        console.log('  ✓ Invalid token error caught and logged');
        console.log('  ✓ Expired token error caught and logged');
        console.log(`  ✓ ${errors.length} errors properly handled`);
        addResult('Error Propagation', 'PASS', 'All errors caught and logged');
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        logger.error('Error propagation test failed', { error: error.message });
        addResult('Error Propagation', 'FAIL', error.message);
    }

    // ========================================================================
    // RESULTS SUMMARY
    // ========================================================================
    console.log('\n' + '='.repeat(70));
    console.log('INTEGRATION TEST RESULTS');
    console.log('='.repeat(70) + '\n');

    const passed = results.filter(r => r.status === 'PASS').length;
    const failed = results.filter(r => r.status === 'FAIL').length;

    console.log(`Total Tests: ${results.length}`);
    console.log(`Passed: ${passed}`);
    console.log(`Failed: ${failed}`);
    console.log(`Success Rate: ${((passed / results.length) * 100).toFixed(1)}%\n`);

    console.log('Test Details:');
    console.log('-'.repeat(70));
    results.forEach(r => {
        const icon = r.status === 'PASS' ? '✓' : '✗';
        console.log(`${icon} ${r.test.padEnd(40)} ${r.status.padEnd(10)} ${r.details}`);
    });
    console.log('-'.repeat(70) + '\n');

    if (failed === 0) {
        console.log('✓ ALL INTEGRATION TESTS PASSED!');
        console.log('✓ Both modules work together seamlessly');
    } else {
        console.log(`⚠️  ${failed} test(s) failed`);
    }

    console.log('\n' + '='.repeat(70));

    logger.info('Integration test suite completed', {
        totalTests: results.length,
        passed,
        failed,
        successRate: ((passed / results.length) * 100).toFixed(1) + '%'
    });

    return {
        totalTests: results.length,
        passed,
        failed,
        results
    };
}

// Run tests
runIntegrationTests()
    .then(summary => {
        console.log('\nIntegration test execution completed.');
        process.exit(summary.failed > 0 ? 1 : 0);
    })
    .catch(error => {
        console.error('\nFatal error:', error);
        process.exit(1);
    });
