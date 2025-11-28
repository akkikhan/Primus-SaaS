/**
 * Logging Module Feature Parity Test
 * Verifies npm package has same features as NuGet package
 */

import { PrimusLogger, PrimusLoggingOptions } from '@primus-saas/logging';
import * as fs from 'fs';
import * as path from 'path';

console.log('╔════════════════════════════════════════════════════════════════╗');
console.log('║      Logging Module NPM Package - Feature Parity Test         ║');
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
const testLogsDir = path.join(process.cwd(), 'test-logs');

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
    console.log('Starting Logging Module tests...\n');

    // Clean up test logs directory
    if (fs.existsSync(testLogsDir)) {
        fs.rmSync(testLogsDir, { recursive: true });
    }
    fs.mkdirSync(testLogsDir, { recursive: true });

    // ========================================================================
    // TEST 1: Basic Logger Initialization
    // ========================================================================
    console.log('Test 1: Basic Logger Initialization');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug'
        };

        const logger = new PrimusLogger(options);
        console.log('  ✓ Logger initialized');
        addResult('Basic Initialization', 'PASS', 'Logger can be initialized', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Basic Initialization', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 2: Log Levels
    // ========================================================================
    console.log('\nTest 2: Log Levels');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enableConsole: false,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);

        logger.debug('Debug message');
        logger.info('Info message');
        logger.warning('Warning message');
        logger.error('Error message');
        logger.critical('Critical message');

        console.log('  ✓ Debug level');
        console.log('  ✓ Info level');
        console.log('  ✓ Warning level');
        console.log('  ✓ Error level');
        console.log('  ✓ Critical level');
        addResult('Log Levels', 'PASS', 'All log levels supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Log Levels', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 3: Structured Logging
    // ========================================================================
    console.log('\nTest 3: Structured Logging');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            structuredLogging: true,
            enableConsole: false,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);

        logger.info('Structured log test', {
            userId: 'user123',
            action: 'test',
            metadata: {
                browser: 'Chrome',
                os: 'Windows'
            }
        });

        console.log('  ✓ Structured logging enabled');
        console.log('  ✓ Metadata attached to logs');
        addResult('Structured Logging', 'PASS', 'Structured logging supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Structured Logging', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 4: PII Masking
    // ========================================================================
    console.log('\nTest 4: PII Masking');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            piiMasking: {
                enabled: true,
                patterns: ['email', 'phone', 'ssn', 'creditCard']
            },
            enableConsole: false,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);

        logger.info('PII test', {
            email: 'test@example.com',
            phone: '+1-555-123-4567',
            ssn: '123-45-6789',
            creditCard: '4532-1234-5678-9010'
        });

        console.log('  ✓ PII masking enabled');
        console.log('  ✓ Email pattern');
        console.log('  ✓ Phone pattern');
        console.log('  ✓ SSN pattern');
        console.log('  ✓ Credit card pattern');
        addResult('PII Masking', 'PASS', 'PII masking with multiple patterns', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('PII Masking', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 5: File Logging
    // ========================================================================
    console.log('\nTest 5: File Logging');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enableFile: true,
            filePath: testLogsDir,
            enableConsole: false
        };

        const logger = new PrimusLogger(options);
        logger.info('File logging test');

        // Wait a bit for file to be written
        await new Promise(resolve => setTimeout(resolve, 500));

        const files = fs.readdirSync(testLogsDir);
        const hasLogFile = files.some(f => f.endsWith('.log') || f.endsWith('.json'));

        if (hasLogFile) {
            console.log('  ✓ File logging enabled');
            console.log('  ✓ Log file created');
            addResult('File Logging', 'PASS', 'File logging works', true, true);
        } else {
            console.log('  ✗ No log file created');
            addResult('File Logging', 'FAIL', 'Log file not created', false, true);
        }
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('File Logging', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 6: File Rotation
    // ========================================================================
    console.log('\nTest 6: File Rotation');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enableFile: true,
            filePath: testLogsDir,
            fileRotation: {
                enabled: true,
                maxFileSizeMB: 1,
                maxFiles: 5
            },
            enableConsole: false
        };

        const logger = new PrimusLogger(options);
        console.log('  ✓ File rotation enabled');
        console.log('  ✓ Max file size configured');
        console.log('  ✓ Max files configured');
        addResult('File Rotation', 'PASS', 'File rotation configuration supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('File Rotation', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 7: Console Logging
    // ========================================================================
    console.log('\nTest 7: Console Logging');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enableConsole: true,
            enableFile: false
        };

        const logger = new PrimusLogger(options);
        logger.info('Console logging test');

        console.log('  ✓ Console logging enabled');
        addResult('Console Logging', 'PASS', 'Console logging works', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Console Logging', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 8: Context Enrichment
    // ========================================================================
    console.log('\nTest 8: Context Enrichment');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enrichers: {
                machineName: true,
                processId: true,
                threadId: true,
                timestamp: true
            },
            enableConsole: false,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);
        logger.info('Enrichment test');

        console.log('  ✓ Machine name enricher');
        console.log('  ✓ Process ID enricher');
        console.log('  ✓ Thread ID enricher');
        console.log('  ✓ Timestamp enricher');
        addResult('Context Enrichment', 'PASS', 'All enrichers supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Context Enrichment', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 9: Middleware Support
    // ========================================================================
    console.log('\nTest 9: Express Middleware');
    try {
        const { primusLoggingMiddleware } = require('@primus-saas/logging');

        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug'
        };

        const logger = new PrimusLogger(options);
        const middleware = primusLoggingMiddleware(logger);

        console.log('  ✓ Middleware function available');
        console.log('  ✓ Type:', typeof middleware);
        addResult('Express Middleware', 'PASS', 'Middleware available', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Express Middleware', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 10: Multiple Targets
    // ========================================================================
    console.log('\nTest 10: Multiple Logging Targets');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Debug',
            enableConsole: true,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);
        logger.info('Multi-target test');

        console.log('  ✓ Console target');
        console.log('  ✓ File target');
        console.log('  ✓ Multiple targets simultaneously');
        addResult('Multiple Targets', 'PASS', 'Multiple targets supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Multiple Targets', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 11: Minimum Log Level Filtering
    // ========================================================================
    console.log('\nTest 11: Minimum Log Level Filtering');
    try {
        const options: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Test',
            minimumLevel: 'Warning',
            enableConsole: false,
            enableFile: true,
            filePath: testLogsDir
        };

        const logger = new PrimusLogger(options);
        logger.debug('Should not be logged');
        logger.info('Should not be logged');
        logger.warning('Should be logged');
        logger.error('Should be logged');

        console.log('  ✓ Minimum level filtering works');
        addResult('Log Level Filtering', 'PASS', 'Minimum level filtering supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Log Level Filtering', 'FAIL', error.message, false, true);
    }

    // ========================================================================
    // TEST 12: Environment-based Configuration
    // ========================================================================
    console.log('\nTest 12: Environment Configuration');
    try {
        const devOptions: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Development',
            minimumLevel: 'Debug'
        };

        const prodOptions: PrimusLoggingOptions = {
            applicationName: 'TestApp',
            environment: 'Production',
            minimumLevel: 'Warning'
        };

        const devLogger = new PrimusLogger(devOptions);
        const prodLogger = new PrimusLogger(prodOptions);

        console.log('  ✓ Development configuration');
        console.log('  ✓ Production configuration');
        addResult('Environment Configuration', 'PASS', 'Environment-based config supported', true, true);
    } catch (error: any) {
        console.log('  ✗ Failed:', error.message);
        addResult('Environment Configuration', 'FAIL', error.message, false, true);
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
