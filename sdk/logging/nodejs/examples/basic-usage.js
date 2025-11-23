const { createLogger, LogLevel } = require('../dist/index');


// Create logger
const logger = createLogger({
    applicationId: 'DEMO-APP-123',
    environment: 'development',
    minLevel: LogLevel.DEBUG
});

console.log('=== Basic Logging Demo ===\n');

// Basic logging
logger.debug('This is a debug message');
logger.info('Application started');
logger.warn('This is a warning');
logger.error('This is an error');
logger.critical('This is critical!');

console.log('\n=== Logging with Context ===\n');

// Logging with context
logger.info('User logged in', {
    userId: '12345',
    username: 'john.doe',
    ipAddress: '192.168.1.1'
});

logger.info('Order created', {
    orderId: 'ORD-789',
    amount: 99.99,
    currency: 'USD'
});

console.log('\n=== Performance Tracking ===\n');

// Performance tracking
const timer = logger.startTimer();

// Simulate some work
setTimeout(() => {
    timer.done('Order processed', { orderId: 'ORD-789' });
}, 100);

console.log('\n=== Correlation IDs ===\n');

// Correlation IDs for microservices
const correlationId = logger.generateCorrelationId();
logger.info('Checkout initiated', { correlationId, cartTotal: 199.99 });
logger.info('Inventory reserved', { correlationId, items: 3 });
logger.info('Payment processed', { correlationId, amount: 199.99 });

console.log('\n=== Log Level Filtering ===\n');

// Create logger with INFO level (DEBUG will be filtered)
const prodLogger = createLogger({
    applicationId: 'PROD-APP-456',
    environment: 'production',
    minLevel: LogLevel.INFO
});

prodLogger.debug('This will NOT be logged');
prodLogger.info('This WILL be logged');
