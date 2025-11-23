import { Logger } from '../Logger';
import { LogLevel } from '../LogLevel';

describe('Logger', () => {
    let logger: Logger;

    beforeEach(() => {
        // Mock console.log to capture output
        jest.spyOn(console, 'log').mockImplementation();
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    describe('Basic Logging', () => {
        beforeEach(() => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing'
            });
        });

        it('should log INFO messages', () => {
            logger.info('Test message', { userId: '123' });

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"level":"INFO"')
            );
            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"message":"Test message"')
            );
            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"userId":"123"')
            );
        });

        it('should log DEBUG messages', () => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing',
                minLevel: LogLevel.DEBUG
            });

            logger.debug('Debug message');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"level":"DEBUG"')
            );
        });

        it('should log WARNING messages', () => {
            logger.warn('Warning message');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"level":"WARNING"')
            );
        });

        it('should log ERROR messages', () => {
            logger.error('Error message');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"level":"ERROR"')
            );
        });

        it('should log CRITICAL messages', () => {
            logger.critical('Critical message');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"level":"CRITICAL"')
            );
        });
    });

    describe('Log Level Filtering', () => {
        it('should filter DEBUG logs when minLevel is INFO', () => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing',
                minLevel: LogLevel.INFO
            });

            logger.debug('This should be filtered');

            expect(console.log).not.toHaveBeenCalled();
        });

        it('should allow INFO logs when minLevel is INFO', () => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing',
                minLevel: LogLevel.INFO
            });

            logger.info('This should be logged');

            expect(console.log).toHaveBeenCalled();
        });

        it('should filter INFO logs when minLevel is ERROR', () => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing',
                minLevel: LogLevel.ERROR
            });

            logger.info('This should be filtered');

            expect(console.log).not.toHaveBeenCalled();
        });
    });

    describe('Context Enrichment', () => {
        beforeEach(() => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'production'
            });
        });

        it('should include applicationId in context', () => {
            logger.info('Test');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"applicationId":"TEST-APP-123"')
            );
        });

        it('should include environment in context', () => {
            logger.info('Test');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"environment":"production"')
            );
        });

        it('should include timestamp', () => {
            logger.info('Test');

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"timestamp"')
            );
        });

        it('should merge custom context', () => {
            logger.info('Test', { userId: '123', action: 'login' });

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"userId":"123"')
            );
            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining('"action":"login"')
            );
        });
    });

    describe('Performance Tracking', () => {
        beforeEach(() => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing'
            });
        });

        it('should track execution time', (done) => {
            const timer = logger.startTimer();

            setTimeout(() => {
                timer.done('Operation completed');

                expect(console.log).toHaveBeenCalledWith(
                    expect.stringContaining('"duration"')
                );
                done();
            }, 10);
        });

        it('should include custom context with timer', (done) => {
            const timer = logger.startTimer();

            setTimeout(() => {
                timer.done('Operation completed', { orderId: '456' });

                expect(console.log).toHaveBeenCalledWith(
                    expect.stringContaining('"orderId":"456"')
                );
                done();
            }, 10);
        });
    });

    describe('Correlation ID', () => {
        beforeEach(() => {
            logger = new Logger({
                applicationId: 'TEST-APP-123',
                environment: 'testing'
            });
        });

        it('should generate unique correlation IDs', () => {
            const id1 = logger.generateCorrelationId();
            const id2 = logger.generateCorrelationId();

            expect(id1).toMatch(/^corr-/);
            expect(id2).toMatch(/^corr-/);
            expect(id1).not.toBe(id2);
        });

        it('should allow logging with correlation ID', () => {
            const correlationId = logger.generateCorrelationId();
            logger.info('Test', { correlationId });

            expect(console.log).toHaveBeenCalledWith(
                expect.stringContaining(`"correlationId":"${correlationId}"`)
            );
        });
    });
});
