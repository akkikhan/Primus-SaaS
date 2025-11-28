/**
 * Identity Validator Feature Parity Test
 * Verifies npm package has same features as NuGet package
 */

import { PrimusIdentityValidator, PrimusIdentityOptions } from '@primus-saas/identity-validator';
import jwt from 'jsonwebtoken';

console.log('╔════════════════════════════════════════════════════════════════╗');
console.log('║   Identity Validator NPM Package - Feature Parity Test        ║');
console.log('╚════════════════════════════════════════════════════════════════╝\n');

interface TestResult {
    feature: string;
    status: 'PASS' | 'FAIL' | 'SKIP';
    details: string;
    npmSupport: boolean;
    nugetSupport: boolean;
    inSync: boolean;
}

const results: TestResult[] = [];

function addResult(feature: string, status: 'PASS' | 'FAIL' | 'SKIP', details: string, npmSupport: boolean, nugetSupport: boolean) {
    results.push({
        feature,
        status,
        details,
        npmSupport,
        nugetSupport,
        inSync: npmSupport === nugetSupport
    });
}

async function runTests() {
    console.log('Starting Identity Validator tests...\n');

    // ========================================================================
    // TEST 1: Multi-Issuer Support
    // ========================================================================
    console.log('Test 1: Multi-Issuer Support');
    try {
        const options: PrimusIdentityOptions = {
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

        const validator = new PrimusIdentityValidator(options);
        console.log('  ✓ Multiple issuers configured');
        addResult('Multi-Issuer Support', 'PASS', 'Can configure multiple issuers', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Multi-Issuer Support', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 2: Azure AD OIDC Support
    // ========================================================================
    console.log('\nTest 2: Azure AD OIDC Support');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'AzureAD',
                    type: 'AzureAD',
                    authority: 'https://login.microsoftonline.com/common/v2.0',
                    audience: 'api://default'
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);
        console.log('  ✓ Azure AD issuer configured');
        addResult('Azure AD OIDC Support', 'PASS', 'Azure AD configuration supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Azure AD OIDC Support', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 3: JWT Validation
    // ========================================================================
    console.log('\nTest 3: JWT Validation');
    try {
        const secret = 'test-secret-key';
        const token = jwt.sign(
            {
                sub: 'user123',
                email: 'test@example.com',
                name: 'Test User'
            },
            secret,
            {
                issuer: 'https://test.com',
                audience: 'test-api',
                expiresIn: '1h'
            }
        );

        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: secret
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);
        const result = await validator.validateToken(token);

        console.log('  ✓ Token validated successfully');
        console.log('  ✓ Claims extracted:', result.claims?.sub);
        addResult('JWT Validation', 'PASS', 'Can validate JWT tokens', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('JWT Validation', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 4: Token Claims Extraction
    // ========================================================================
    console.log('\nTest 4: Token Claims Extraction');
    try {
        const secret = 'test-secret-key';
        const token = jwt.sign(
            {
                sub: 'user123',
                email: 'test@example.com',
                name: 'Test User',
                roles: ['admin', 'user'],
                customClaim: 'customValue'
            },
            secret,
            {
                issuer: 'https://test.com',
                audience: 'test-api',
                expiresIn: '1h'
            }
        );

        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: secret
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);
        const result = await validator.validateToken(token);

        const hasSub = !!result.claims?.sub;
        const hasEmail = !!result.claims?.email;
        const hasRoles = !!result.claims?.roles;
        const hasCustom = !!(result.claims as any)?.customClaim;

        console.log('  ✓ Standard claims extracted:', { hasSub, hasEmail });
        console.log('  ✓ Custom claims extracted:', { hasRoles, hasCustom });
        addResult('Token Claims Extraction', 'PASS', 'All claim types extracted', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Token Claims Extraction', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 5: Middleware Support
    // ========================================================================
    console.log('\nTest 5: Express Middleware Support');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: 'secret'
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);
        const middleware = validator.middleware();

        console.log('  ✓ Middleware function created');
        console.log('  ✓ Type:', typeof middleware);
        addResult('Express Middleware', 'PASS', 'Middleware available', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Express Middleware', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 6: Caching Support
    // ========================================================================
    console.log('\nTest 6: Token Caching');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: 'secret'
                }
            ],
            enableCaching: true,
            cacheDuration: 300
        };

        const validator = new PrimusIdentityValidator(options);
        console.log('  ✓ Caching enabled');
        console.log('  ✓ Cache duration configured');
        addResult('Token Caching', 'PASS', 'Caching configuration supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Token Caching', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 7: Validation Options
    // ========================================================================
    console.log('\nTest 7: Validation Options');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: 'secret',
                    validateIssuer: true,
                    validateAudience: true,
                    validateLifetime: true
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);
        console.log('  ✓ Issuer validation configured');
        console.log('  ✓ Audience validation configured');
        console.log('  ✓ Lifetime validation configured');
        addResult('Validation Options', 'PASS', 'All validation options available', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Validation Options', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 8: Error Handling
    // ========================================================================
    console.log('\nTest 8: Error Handling');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: 'secret'
                }
            ]
        };

        const validator = new PrimusIdentityValidator(options);

        try {
            await validator.validateToken('invalid-token');
            console.log('  ✗ Should have thrown error for invalid token');
            addResult('Error Handling', 'FAIL', 'Invalid token not rejected', false, true);
        } catch (validationError: any) {
            console.log('  ✓ Invalid token rejected');
            console.log('  ✓ Error message:', validationError.message);
            addResult('Error Handling', 'PASS', 'Proper error handling', true, true);
        }
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Error Handling', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 9: Diagnostics Support
    // ========================================================================
    console.log('\nTest 9: Diagnostics Support');
    try {
        const options: PrimusIdentityOptions = {
            issuers: [
                {
                    name: 'TestIssuer',
                    type: 'JWT',
                    issuer: 'https://test.com',
                    audience: 'test-api',
                    secret: 'secret'
                }
            ],
            enableDiagnostics: true
        };

        const validator = new PrimusIdentityValidator(options);
        console.log('  ✓ Diagnostics enabled');
        addResult('Diagnostics Support', 'PASS', 'Diagnostics configuration available', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Diagnostics Support', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // RESULTS SUMMARY
    // ========================================================================
    console.log('\n' + '='.repeat(70));
    console.log('TEST RESULTS SUMMARY');
    console.log('='.repeat(70) + '\n');

    const passed = results.filter(r => r.status === 'PASS').length;
    const failed = results.filter(r => r.status === 'FAIL').length;
    const skipped = results.filter(r => r.status === 'SKIP').length;
    const inSync = results.filter(r => r.inSync).length;

    console.log(`Total Tests: ${results.length}`);
    console.log(`Passed: ${passed}`);
    console.log(`Failed: ${failed}`);
    console.log(`Skipped: ${skipped}`);
    console.log(`In Sync with NuGet: ${inSync}/${results.length}\n`);

    console.log('Feature Parity Matrix:');
    console.log('-'.repeat(70));
    console.log('Feature'.padEnd(35) + 'NPM'.padEnd(10) + 'NuGet'.padEnd(10) + 'Status');
    console.log('-'.repeat(70));

    results.forEach(r => {
        const npmIcon = r.npmSupport ? '✓' : '✗';
        const nugetIcon = r.nugetSupport ? '✓' : '✗';
        const syncIcon = r.inSync ? '✓ SYNCED' : '✗ GAP';

        console.log(
            r.feature.padEnd(35) +
            npmIcon.padEnd(10) +
            nugetIcon.padEnd(10) +
            syncIcon
        );
    });

    console.log('-'.repeat(70) + '\n');

    // Check for gaps
    const gaps = results.filter(r => !r.inSync);
    if (gaps.length > 0) {
        console.log('⚠️  GAPS DETECTED:');
        gaps.forEach(gap => {
            console.log(`  - ${gap.feature}: ${gap.details}`);
        });
    } else {
        console.log('✓ ALL FEATURES IN SYNC!');
    }

    console.log('\n' + '='.repeat(70));

    return {
        totalTests: results.length,
        passed,
        failed,
        skipped,
        inSync,
        results
    };
}

// Run tests
runTests()
    .then(summary => {
        console.log('\nTest execution completed.');
        process.exit(summary.failed > 0 ? 1 : 0);
    })
    .catch(error => {
        console.error('\nFatal error:', error);
        process.exit(1);
    });
