/**
 * Real-World Demo Application
 * E-Commerce API with Authentication and Logging
 */

import express, { Request, Response, NextFunction } from 'express';
import dotenv from 'dotenv';
import jwt from 'jsonwebtoken';
import { PrimusIdentityValidator, PrimusIdentityOptions, primusIdentityMiddleware } from '@primus-saas/identity-validator';
import { Logger, LoggerOptions, LogLevel, createLogger, primusLoggingMiddleware } from '@primus-saas/logging';

// Load environment variables
dotenv.config();

const app = express();
const PORT = process.env.PORT || 4000;

// ============================================================================
// LOGGING SETUP
// ============================================================================
const loggingOptions: LoggerOptions = {
    applicationId: process.env.APP_NAME || 'primus-demo',
    environment: (process.env.NODE_ENV as any) || 'development',
    minLevel: LogLevel.DEBUG,
    targets: [
        { type: 'console', pretty: true },
        { type: 'file', path: './logs', async: true }
    ],
    masking: {
        enabled: true,
        maskEmails: true,
        maskCreditCards: true,
        maskSSN: true
    }
};

const logger = createLogger(loggingOptions);
logger.info('Application starting...', {
    port: PORT,
    environment: process.env.NODE_ENV
});

// ============================================================================
// IDENTITY VALIDATOR SETUP
// ============================================================================
const identityOptions: PrimusIdentityOptions = {
    issuers: [{
        name: 'DemoIssuer',
        type: 'jwt',
        issuer: process.env.JWT_ISSUER || 'https://primus-demo.com',
        audiences: [process.env.JWT_AUDIENCE || 'primus-demo-api'],
        secret: process.env.JWT_SECRET || 'default-secret-change-me'
    }]
};

const identityValidator = new PrimusIdentityValidator(identityOptions);

// ============================================================================
// MIDDLEWARE
// ============================================================================
app.use(express.json());
app.use(primusLoggingMiddleware(logger));

// Request logging middleware
app.use((req: Request, res: Response, next: NextFunction) => {
    const start = Date.now();

    res.on('finish', () => {
        const duration = Date.now() - start;
        logger.info('Request completed', {
            method: req.method,
            path: req.path,
            statusCode: res.statusCode,
            duration: `${duration}ms`,
            ip: req.ip
        });
    });

    next();
});

// ============================================================================
// IN-MEMORY DATA STORE (Simulating a database)
// ============================================================================
interface User {
    id: string;
    email: string;
    name: string;
    password: string; // In real app, this would be hashed
    role: string;
}

interface Product {
    id: string;
    name: string;
    price: number;
    description: string;
    stock: number;
}

interface Order {
    id: string;
    userId: string;
    products: { productId: string; quantity: number }[];
    total: number;
    status: 'pending' | 'completed' | 'cancelled';
    createdAt: Date;
}

const users: User[] = [
    { id: '1', email: 'admin@demo.com', name: 'Admin User', password: 'admin123', role: 'admin' },
    { id: '2', email: 'user@demo.com', name: 'Regular User', password: 'user123', role: 'user' }
];

const products: Product[] = [
    { id: '1', name: 'Laptop', price: 999.99, description: 'High-performance laptop', stock: 10 },
    { id: '2', name: 'Mouse', price: 29.99, description: 'Wireless mouse', stock: 50 },
    { id: '3', name: 'Keyboard', price: 79.99, description: 'Mechanical keyboard', stock: 30 }
];

const orders: Order[] = [];

// ============================================================================
// PUBLIC ROUTES
// ============================================================================

app.get('/', (req: Request, res: Response) => {
    logger.info('Root endpoint accessed');
    res.json({
        message: 'Primus SaaS Real-World Demo API',
        version: '1.0.0',
        endpoints: {
            auth: {
                login: 'POST /auth/login',
                register: 'POST /auth/register'
            },
            products: {
                list: 'GET /products',
                get: 'GET /products/:id'
            },
            orders: {
                create: 'POST /orders (requires auth)',
                list: 'GET /orders (requires auth)',
                get: 'GET /orders/:id (requires auth)'
            },
            admin: {
                users: 'GET /admin/users (requires admin role)',
                stats: 'GET /admin/stats (requires admin role)'
            }
        }
    });
});

// ============================================================================
// AUTHENTICATION ROUTES
// ============================================================================

app.post('/auth/register', (req: Request, res: Response) => {
    const { email, name, password } = req.body;

    logger.info('Registration attempt', { email });

    if (!email || !name || !password) {
        logger.warn('Registration failed: missing fields', { email });
        return res.status(400).json({ error: 'Email, name, and password are required' });
    }

    // Check if user already exists
    if (users.find(u => u.email === email)) {
        logger.warn('Registration failed: email already exists', { email });
        return res.status(409).json({ error: 'Email already registered' });
    }

    // Create new user
    const newUser: User = {
        id: String(users.length + 1),
        email,
        name,
        password, // In real app, hash this!
        role: 'user'
    };

    users.push(newUser);

    logger.info('User registered successfully', {
        userId: newUser.id,
        email: newUser.email
    });

    res.status(201).json({
        message: 'User registered successfully',
        user: {
            id: newUser.id,
            email: newUser.email,
            name: newUser.name,
            role: newUser.role
        }
    });
});

app.post('/auth/login', (req: Request, res: Response) => {
    const { email, password } = req.body;

    logger.info('Login attempt', { email });

    if (!email || !password) {
        logger.warn('Login failed: missing credentials', { email });
        return res.status(400).json({ error: 'Email and password are required' });
    }

    // Find user
    const user = users.find(u => u.email === email && u.password === password);

    if (!user) {
        logger.warn('Login failed: invalid credentials', { email });
        return res.status(401).json({ error: 'Invalid email or password' });
    }

    // Generate JWT token
    const token = jwt.sign(
        {
            sub: user.id,
            email: user.email,
            name: user.name,
            role: user.role
        },
        process.env.JWT_SECRET || 'default-secret',
        {
            issuer: process.env.JWT_ISSUER || 'https://primus-demo.com',
            audience: process.env.JWT_AUDIENCE || 'primus-demo-api',
            expiresIn: '24h'
        }
    );

    logger.info('Login successful', {
        userId: user.id,
        email: user.email,
        role: user.role
    });

    res.json({
        message: 'Login successful',
        token,
        user: {
            id: user.id,
            email: user.email,
            name: user.name,
            role: user.role
        }
    });
});

// ============================================================================
// PUBLIC PRODUCT ROUTES
// ============================================================================

app.get('/products', (req: Request, res: Response) => {
    logger.info('Products list requested');
    res.json({
        products: products.map(p => ({
            id: p.id,
            name: p.name,
            price: p.price,
            description: p.description,
            inStock: p.stock > 0
        }))
    });
});

app.get('/products/:id', (req: Request, res: Response) => {
    const { id } = req.params;
    logger.info('Product details requested', { productId: id });

    const product = products.find(p => p.id === id);

    if (!product) {
        logger.warn('Product not found', { productId: id });
        return res.status(404).json({ error: 'Product not found' });
    }

    res.json({ product });
});

// ============================================================================
// PROTECTED ROUTES (Require Authentication)
// ============================================================================

// Apply authentication middleware to all routes below
app.use(primusIdentityMiddleware(identityOptions));

app.post('/orders', (req: Request, res: Response) => {
    const user = (req as any).primusUser;
    const { items } = req.body; // items: [{ productId, quantity }]

    logger.info('Order creation attempt', {
        userId: user.userId,
        itemCount: items?.length
    });

    if (!items || !Array.isArray(items) || items.length === 0) {
        logger.warn('Order creation failed: invalid items', { userId: user.userId });
        return res.status(400).json({ error: 'Items array is required' });
    }

    // Calculate total and validate products
    let total = 0;
    const orderProducts: { productId: string; quantity: number }[] = [];

    for (const item of items) {
        const product = products.find(p => p.id === item.productId);

        if (!product) {
            logger.warn('Order creation failed: invalid product', {
                userId: user.userId,
                productId: item.productId
            });
            return res.status(400).json({
                error: `Product ${item.productId} not found`
            });
        }

        if (product.stock < item.quantity) {
            logger.warn('Order creation failed: insufficient stock', {
                userId: user.userId,
                productId: item.productId,
                requested: item.quantity,
                available: product.stock
            });
            return res.status(400).json({
                error: `Insufficient stock for ${product.name}`
            });
        }

        total += product.price * item.quantity;
        orderProducts.push({ productId: item.productId, quantity: item.quantity });

        // Update stock
        product.stock -= item.quantity;
    }

    // Create order
    const newOrder: Order = {
        id: String(orders.length + 1),
        userId: user.userId,
        products: orderProducts,
        total,
        status: 'pending',
        createdAt: new Date()
    };

    orders.push(newOrder);

    logger.info('Order created successfully', {
        orderId: newOrder.id,
        userId: user.userId,
        total: newOrder.total,
        itemCount: orderProducts.length
    });

    res.status(201).json({
        message: 'Order created successfully',
        order: newOrder
    });
});

app.get('/orders', (req: Request, res: Response) => {
    const user = (req as any).primusUser;

    logger.info('Orders list requested', { userId: user.userId });

    const userOrders = orders.filter(o => o.userId === user.userId);

    res.json({
        orders: userOrders
    });
});

app.get('/orders/:id', (req: Request, res: Response) => {
    const user = (req as any).primusUser;
    const { id } = req.params;

    logger.info('Order details requested', {
        userId: user.userId,
        orderId: id
    });

    const order = orders.find(o => o.id === id && o.userId === user.userId);

    if (!order) {
        logger.warn('Order not found or unauthorized', {
            userId: user.userId,
            orderId: id
        });
        return res.status(404).json({ error: 'Order not found' });
    }

    res.json({ order });
});

// ============================================================================
// ADMIN ROUTES (Require admin role)
// ============================================================================

// Admin role check middleware
const requireAdmin = (req: Request, res: Response, next: NextFunction) => {
    const user = (req as any).primusUser;

    if (!user.roles.includes('admin')) {
        logger.warn('Admin access denied', {
            userId: user.userId,
            roles: user.roles
        });
        return res.status(403).json({ error: 'Admin access required' });
    }

    next();
};

app.get('/admin/users', requireAdmin, (req: Request, res: Response) => {
    const admin = (req as any).primusUser;

    logger.info('Admin: Users list requested', { adminId: admin.userId });

    res.json({
        users: users.map(u => ({
            id: u.id,
            email: u.email,
            name: u.name,
            role: u.role
        }))
    });
});

app.get('/admin/stats', requireAdmin, (req: Request, res: Response) => {
    const admin = (req as any).primusUser;

    logger.info('Admin: Stats requested', { adminId: admin.userId });

    const totalRevenue = orders.reduce((sum, order) => sum + order.total, 0);
    const completedOrders = orders.filter(o => o.status === 'completed').length;

    res.json({
        stats: {
            totalUsers: users.length,
            totalProducts: products.length,
            totalOrders: orders.length,
            completedOrders,
            pendingOrders: orders.filter(o => o.status === 'pending').length,
            totalRevenue: totalRevenue.toFixed(2)
        }
    });
});

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
        error: 'Internal server error',
        message: err.message
    });
});

// ============================================================================
// START SERVER
// ============================================================================

app.listen(PORT, () => {
    logger.info('Server started successfully', {
        port: PORT,
        environment: process.env.NODE_ENV,
        endpoints: {
            public: ['/', '/auth/login', '/auth/register', '/products'],
            protected: ['/orders'],
            admin: ['/admin/users', '/admin/stats']
        }
    });

    console.log(`
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║        Primus SaaS Real-World Demo API                        ║
║                                                                ║
║  Server running on: http://localhost:${PORT}                     ║
║                                                                ║
║  Modules Active:                                               ║
║    ✓ @primus-saas/identity-validator                          ║
║    ✓ @primus-saas/logging                                     ║
║                                                                ║
║  Test Users:                                                   ║
║    Admin: admin@demo.com / admin123                           ║
║    User:  user@demo.com / user123                             ║
║                                                                ║
║  Try these commands:                                           ║
║    curl http://localhost:${PORT}                                 ║
║    curl http://localhost:${PORT}/products                        ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
    `);
});

export default app;
