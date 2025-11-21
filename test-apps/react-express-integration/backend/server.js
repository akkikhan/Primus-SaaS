import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { primusIdentityMiddleware, requireRoles } from 'primus-identity-validator';

// Load environment variables
dotenv.config();

const app = express();
const PORT = process.env.PORT || 3001;

// ========================================
// MIDDLEWARE
// ========================================

// Enable CORS for frontend
app.use(cors({
  origin: process.env.FRONTEND_URL || 'http://localhost:5173',
  credentials: true
}));

// Parse JSON request bodies
app.use(express.json());

// ========================================
// CONFIGURE PRIMUS AUTHENTICATION
// ========================================

console.log('🔐 Configuring Primus Authentication...');
console.log('   Portal URL:', process.env.PRIMUS_PORTAL_URL);
console.log('   Client ID:', process.env.PRIMUS_CLIENT_ID);
console.log('   Validation Mode:', process.env.PRIMUS_VALIDATION_MODE);

const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL,
  clientId: process.env.PRIMUS_CLIENT_ID,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET,
  mode: process.env.PRIMUS_VALIDATION_MODE || 'Local',
  jwtSecret: process.env.PRIMUS_JWT_SECRET,
});

// ========================================
// PUBLIC ROUTES (No authentication needed)
// ========================================

app.get('/api/health', (req, res) => {
  res.json({ 
    status: 'healthy', 
    timestamp: new Date().toISOString(),
    message: 'Backend is running!' 
  });
});

app.get('/api/public-data', (req, res) => {
  res.json({ 
    message: 'This is public data',
    data: 'Anyone can access this without logging in',
    tip: 'Try accessing /api/profile - it requires authentication!'
  });
});

// ========================================
// PROTECTED ROUTES (Authentication required)
// ========================================

// Get current user profile
app.get('/api/profile', primusAuth, (req, res) => {
  console.log('✅ Authenticated user:', req.user?.email);
  
  res.json({
    success: true,
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles,
      tenantId: req.user?.tenantId,
    }
  });
});

// Dashboard data - requires authentication
app.get('/api/dashboard', primusAuth, (req, res) => {
  res.json({
    message: 'Welcome to your dashboard',
    user: req.user?.email,
    data: {
      stats: {
        totalUsers: 150,
        activeProjects: 23,
        completedTasks: 89
      },
      recentActivity: [
        { action: 'Login', time: '2 minutes ago' },
        { action: 'Updated profile', time: '1 hour ago' },
        { action: 'Created project', time: '3 hours ago' }
      ]
    }
  });
});

// ========================================
// ADMIN ROUTES (Admin role required)
// ========================================

// Admin - List all users
app.get('/api/admin/users', 
  primusAuth, 
  requireRoles('Admin'), 
  (req, res) => {
    console.log('🔐 Admin access granted to:', req.user?.email);
    
    res.json({
      message: 'Admin users list',
      users: [
        { id: 1, email: 'user1@example.com', role: 'User', status: 'Active' },
        { id: 2, email: 'user2@example.com', role: 'Manager', status: 'Active' },
        { id: 3, email: 'admin@primussaas.com', role: 'Admin', status: 'Active' }
      ]
    });
  }
);

// Admin - System settings
app.get('/api/admin/settings', 
  primusAuth, 
  requireRoles('Admin'), 
  (req, res) => {
    res.json({
      settings: {
        siteName: 'Primus Integration Demo',
        theme: 'dark',
        maintenanceMode: false,
        registrationEnabled: true
      }
    });
  }
);

// ========================================
// MANAGER/ADMIN ROUTES (Multiple roles allowed)
// ========================================

// Reports - accessible by Manager or Admin
app.get('/api/reports', 
  primusAuth, 
  requireRoles('Manager', 'Admin'), 
  (req, res) => {
    res.json({
      message: 'Reports data',
      reports: [
        { id: 1, name: 'Sales Report Q4 2025', date: '2025-11-21', status: 'Complete' },
        { id: 2, name: 'User Analytics', date: '2025-11-20', status: 'Complete' },
        { id: 3, name: 'Performance Metrics', date: '2025-11-19', status: 'Complete' }
      ]
    });
  }
);

// ========================================
// ERROR HANDLING
// ========================================

app.use((err, req, res, next) => {
  console.error('❌ Error:', err.message);
  
  // Unauthorized - missing or invalid token
  if (err.status === 401) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Please login to access this resource',
      tip: 'Include a valid JWT token in the Authorization header'
    });
  }
  
  // Forbidden - insufficient permissions
  if (err.status === 403) {
    return res.status(403).json({
      error: 'Forbidden',
      message: 'You do not have permission to access this resource',
      requiredRole: err.requiredRole || 'Unknown'
    });
  }
  
  // Generic server error
  res.status(err.status || 500).json({
    error: 'Server Error',
    message: err.message
  });
});

// ========================================
// START SERVER
// ========================================

app.listen(PORT, () => {
  console.log('\n' + '='.repeat(50));
  console.log('🚀 Primus Integration Backend Started!');
  console.log('='.repeat(50));
  console.log(`📡 Server: http://localhost:${PORT}`);
  console.log(`🔐 Auth Mode: ${process.env.PRIMUS_VALIDATION_MODE}`);
  console.log(`🌐 Frontend: ${process.env.FRONTEND_URL}`);
  console.log('\n📍 Available Endpoints:');
  console.log('   PUBLIC:');
  console.log('   - GET  /api/health         (Health check)');
  console.log('   - GET  /api/public-data    (No auth needed)');
  console.log('\n   PROTECTED:');
  console.log('   - GET  /api/profile        (Requires login)');
  console.log('   - GET  /api/dashboard      (Requires login)');
  console.log('\n   ADMIN ONLY:');
  console.log('   - GET  /api/admin/users    (Requires Admin role)');
  console.log('   - GET  /api/admin/settings (Requires Admin role)');
  console.log('\n   MANAGER/ADMIN:');
  console.log('   - GET  /api/reports        (Requires Manager or Admin)');
  console.log('='.repeat(50) + '\n');
});
