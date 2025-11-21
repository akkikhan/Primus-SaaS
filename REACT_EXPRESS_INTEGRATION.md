# 🚀 React + Express Integration Guide - Primus SaaS

Complete step-by-step guide to integrate Primus Identity Validator into your existing React frontend and Express backend application.

**Target**: Developers with existing React + Express apps who have Primus Client ID and Secret

---

## 📋 What You Need

✅ Primus Client ID (from Primus Portal)  
✅ Primus Client Secret (from Primus Portal)  
✅ Existing React frontend  
✅ Existing Express backend  
✅ 20 minutes of your time

---

## 🎯 What You'll Achieve

After this guide, your app will have:
- ✅ Secure JWT authentication
- ✅ Protected API endpoints
- ✅ Role-based access control
- ✅ Login/logout functionality
- ✅ User session management

---

# Part 1: Backend Integration (Express)

## Step 1: Install the SDK

In your Express backend directory:

```bash
npm install primus-identity-validator
```

## Step 2: Create `.env` File

Create or update `.env` in your backend root:

```env
# Server
PORT=3001
NODE_ENV=development

# Primus Configuration
PRIMUS_PORTAL_URL=http://localhost:5000
PRIMUS_CLIENT_ID=your-actual-client-id-here
PRIMUS_CLIENT_SECRET=your-actual-client-secret-here
PRIMUS_VALIDATION_MODE=Local
PRIMUS_JWT_SECRET=your-super-secret-jwt-key-at-least-32-characters-long

# Frontend URL (for CORS)
FRONTEND_URL=http://localhost:5173
```

**Where to get these values:**
- `PRIMUS_CLIENT_ID`: From Application details in Primus Portal
- `PRIMUS_CLIENT_SECRET`: From Application details in Primus Portal (click "Show Secret")
- `PRIMUS_JWT_SECRET`: Generate a random 32+ character string

## Step 3: Update Your Express Server

Create or update your main server file (e.g., `server.js` or `index.ts`):

```javascript
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

app.use(cors({
  origin: process.env.FRONTEND_URL || 'http://localhost:5173',
  credentials: true
}));

app.use(express.json());

// ========================================
// CONFIGURE PRIMUS AUTHENTICATION
// ========================================

const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL,
  clientId: process.env.PRIMUS_CLIENT_ID,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET,
  mode: process.env.PRIMUS_VALIDATION_MODE || 'Local',
  jwtSecret: process.env.PRIMUS_JWT_SECRET,
});

// ========================================
// PUBLIC ROUTES (No auth needed)
// ========================================

app.get('/api/health', (req, res) => {
  res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

app.get('/api/public-data', (req, res) => {
  res.json({ 
    message: 'This endpoint is public',
    data: 'Anyone can access this'
  });
});

// ========================================
// PROTECTED ROUTES (Auth required)
// ========================================

app.get('/api/profile', primusAuth, (req, res) => {
  res.json({
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles,
    }
  });
});

app.get('/api/dashboard', primusAuth, (req, res) => {
  res.json({
    message: 'Dashboard data',
    user: req.user?.email,
    data: {
      stats: { totalUsers: 150, activeProjects: 23 },
      recentActivity: ['Login', 'Updated profile', 'Created project']
    }
  });
});

// ========================================
// ADMIN ROUTES (Admin role required)
// ========================================

app.get('/api/admin/users', 
  primusAuth, 
  requireRoles('Admin'), 
  (req, res) => {
    res.json({
      users: [
        { id: 1, email: 'user1@example.com', role: 'User' },
        { id: 2, email: 'user2@example.com', role: 'Manager' }
      ]
    });
  }
);

app.get('/api/admin/settings', 
  primusAuth, 
  requireRoles('Admin'), 
  (req, res) => {
    res.json({
      settings: {
        siteName: 'My App',
        theme: 'dark',
        maintenanceMode: false
      }
    });
  }
);

// ========================================
// MANAGER/ADMIN ROUTES
// ========================================

app.get('/api/reports', 
  primusAuth, 
  requireRoles('Manager', 'Admin'), 
  (req, res) => {
    res.json({
      reports: [
        { id: 1, name: 'Sales Report', date: '2025-11-21' },
        { id: 2, name: 'User Analytics', date: '2025-11-20' }
      ]
    });
  }
);

// ========================================
// ERROR HANDLING
// ========================================

app.use((err, req, res, next) => {
  console.error('Error:', err.message);
  
  if (err.status === 401) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Please login to access this resource'
    });
  }
  
  if (err.status === 403) {
    return res.status(403).json({
      error: 'Forbidden',
      message: 'You do not have permission to access this resource'
    });
  }
  
  res.status(err.status || 500).json({
    error: 'Server Error',
    message: err.message
  });
});

// ========================================
// START SERVER
// ========================================

app.listen(PORT, () => {
  console.log(`✅ Server running on http://localhost:${PORT}`);
  console.log(`🔐 Primus Auth Mode: ${process.env.PRIMUS_VALIDATION_MODE}`);
  console.log(`🌐 Frontend URL: ${process.env.FRONTEND_URL}`);
});
```

## Step 4: Test Your Backend

Start your server:

```bash
npm run dev
```

Test without authentication (should work):
```bash
curl http://localhost:3001/api/health
curl http://localhost:3001/api/public-data
```

Test with authentication (should fail):
```bash
curl http://localhost:3001/api/profile
# Expected Response: {"error":"Unauthorized","message":"Please login to access this resource"}
```

✅ **Backend is ready!**

---

# Part 2: Frontend Integration (React)

## Step 1: Install Axios

In your React frontend directory:

```bash
npm install axios
```

## Step 2: Create Environment File

Create `.env` in your React project root:

```env
VITE_API_URL=http://localhost:3001
VITE_PRIMUS_PORTAL_URL=http://localhost:5000
```

## Step 3: Create API Service

Create `src/services/api.js`:

```javascript
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:3001';
const PORTAL_URL = import.meta.env.VITE_PRIMUS_PORTAL_URL || 'http://localhost:5000';

class ApiService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Load token from localStorage
    this.token = localStorage.getItem('auth_token');
    if (this.token) {
      this.setAuthToken(this.token);
    }

    // Auto-redirect on 401
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.clearAuth();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  setAuthToken(token) {
    this.token = token;
    this.api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    localStorage.setItem('auth_token', token);
  }

  clearAuth() {
    this.token = null;
    delete this.api.defaults.headers.common['Authorization'];
    localStorage.removeItem('auth_token');
  }

  isAuthenticated() {
    return !!this.token;
  }

  // ===== AUTHENTICATION =====
  
  async login(email, password) {
    const response = await axios.post(`${PORTAL_URL}/api/auth/login`, {
      email,
      password
    });
    
    const { token } = response.data;
    this.setAuthToken(token);
    return response.data;
  }

  logout() {
    this.clearAuth();
  }

  // ===== USER ENDPOINTS =====

  async getProfile() {
    const response = await this.api.get('/api/profile');
    return response.data;
  }

  async getDashboard() {
    const response = await this.api.get('/api/dashboard');
    return response.data;
  }

  // ===== ADMIN ENDPOINTS =====

  async getAdminUsers() {
    const response = await this.api.get('/api/admin/users');
    return response.data;
  }

  async getAdminSettings() {
    const response = await this.api.get('/api/admin/settings');
    return response.data;
  }

  // ===== REPORTS =====

  async getReports() {
    const response = await this.api.get('/api/reports');
    return response.data;
  }
}

export const apiService = new ApiService();
```

## Step 4: Create Auth Context

Create `src/contexts/AuthContext.jsx`:

```javascript
import { createContext, useContext, useState, useEffect } from 'react';
import { apiService } from '../services/api';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check if user is logged in on mount
  useEffect(() => {
    const checkAuth = async () => {
      if (apiService.isAuthenticated()) {
        try {
          const profileData = await apiService.getProfile();
          setUser(profileData.user);
        } catch (error) {
          console.error('Auth check failed:', error);
          apiService.clearAuth();
        }
      }
      setIsLoading(false);
    };

    checkAuth();
  }, []);

  const login = async (email, password) => {
    await apiService.login(email, password);
    const profileData = await apiService.getProfile();
    setUser(profileData.user);
  };

  const logout = () => {
    apiService.logout();
    setUser(null);
  };

  const hasRole = (role) => {
    return user?.roles?.includes(role) || false;
  };

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user,
      isLoading,
      login,
      logout,
      hasRole,
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

## Step 5: Create Protected Route Component

Create `src/components/ProtectedRoute.jsx`:

```javascript
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export const ProtectedRoute = ({ children, requiredRole }) => {
  const { isAuthenticated, isLoading, hasRole } = useAuth();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && !hasRole(requiredRole)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return children;
};
```

## Step 6: Create Login Page

Create `src/pages/LoginPage.jsx`:

```javascript
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export const LoginPage = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '400px', margin: '100px auto', padding: '30px', border: '1px solid #ddd', borderRadius: '8px' }}>
      <h1>Login</h1>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px' }}>Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            style={{ width: '100%', padding: '10px', fontSize: '16px' }}
          />
        </div>

        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '5px' }}>Password</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            style={{ width: '100%', padding: '10px', fontSize: '16px' }}
          />
        </div>

        {error && (
          <div style={{ color: 'red', marginBottom: '20px' }}>
            {error}
          </div>
        )}

        <button
          type="submit"
          disabled={isLoading}
          style={{
            width: '100%',
            padding: '12px',
            fontSize: '16px',
            backgroundColor: '#007bff',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: isLoading ? 'not-allowed' : 'pointer',
          }}
        >
          {isLoading ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </div>
  );
};
```

## Step 7: Create Dashboard Page

Create `src/pages/DashboardPage.jsx`:

```javascript
import { useEffect, useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { apiService } from '../services/api';
import { useNavigate } from 'react-router-dom';

export const DashboardPage = () => {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();
  const [dashboardData, setDashboardData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const data = await apiService.getDashboard();
        setDashboardData(data);
      } catch (error) {
        console.error('Failed to fetch dashboard:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div style={{ padding: '30px' }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '30px' }}>
        <h1>Dashboard</h1>
        <button onClick={handleLogout} style={{ padding: '10px 20px' }}>
          Logout
        </button>
      </header>

      <div style={{ marginBottom: '20px', padding: '20px', backgroundColor: '#f5f5f5', borderRadius: '8px' }}>
        <h2>👤 User Info</h2>
        <p><strong>Email:</strong> {user?.email}</p>
        <p><strong>User ID:</strong> {user?.userId}</p>
        <p><strong>Roles:</strong> {user?.roles.join(', ')}</p>
      </div>

      <div style={{ marginBottom: '20px', padding: '20px', backgroundColor: '#f5f5f5', borderRadius: '8px' }}>
        <h2>📊 Dashboard Data</h2>
        <pre>{JSON.stringify(dashboardData, null, 2)}</pre>
      </div>

      <div style={{ display: 'flex', gap: '10px' }}>
        {hasRole('Admin') && (
          <button 
            onClick={() => navigate('/admin')} 
            style={{ padding: '10px 20px', backgroundColor: '#dc3545', color: 'white', border: 'none', borderRadius: '4px' }}
          >
            Admin Panel
          </button>
        )}

        {(hasRole('Manager') || hasRole('Admin')) && (
          <button 
            onClick={() => navigate('/reports')} 
            style={{ padding: '10px 20px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '4px' }}
          >
            View Reports
          </button>
        )}
      </div>
    </div>
  );
};
```

## Step 8: Update App.jsx

Update your `src/App.jsx`:

```javascript
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/admin"
            element={
              <ProtectedRoute requiredRole="Admin">
                <div style={{ padding: '30px' }}>
                  <h1>Admin Panel</h1>
                  <p>Only admins can see this</p>
                </div>
              </ProtectedRoute>
            }
          />

          <Route
            path="/reports"
            element={
              <ProtectedRoute>
                <div style={{ padding: '30px' }}>
                  <h1>Reports</h1>
                  <p>Managers and Admins can see this</p>
                </div>
              </ProtectedRoute>
            }
          />

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          
          <Route 
            path="/unauthorized" 
            element={
              <div style={{ padding: '30px', textAlign: 'center' }}>
                <h1>⛔ Unauthorized</h1>
                <p>You don't have permission to access this page</p>
              </div>
            } 
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
```

## Step 9: Start Your Frontend

```bash
npm run dev
```

✅ **Frontend is ready!**

---

# Part 3: Test Everything

## Test Flow

1. **Start Backend**: `npm run dev` in backend directory
2. **Start Frontend**: `npm run dev` in frontend directory
3. **Open Browser**: Navigate to `http://localhost:5173`

## Test Scenarios

### ✅ Test 1: Login
1. Go to `http://localhost:5173/login`
2. Enter credentials:
   - Email: `admin@primussaas.com`
   - Password: `Admin123!`
3. Click "Login"
4. Should redirect to `/dashboard`

### ✅ Test 2: View Dashboard
1. After login, you should see:
   - Your user email
   - Your user ID
   - Your roles
   - Dashboard data

### ✅ Test 3: Admin Access
1. If you have Admin role, click "Admin Panel"
2. Should see admin-only content
3. If not admin, should redirect to `/unauthorized`

### ✅ Test 4: Logout
1. Click "Logout" button
2. Should redirect to `/login`
3. Token should be cleared from localStorage

### ✅ Test 5: Protected Route Without Auth
1. Logout first
2. Try to access `http://localhost:5173/dashboard` directly
3. Should redirect to `/login`

---

# 🧪 Testing with curl

## Get a Token

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@primussaas.com","password":"Admin123!"}'
```

Copy the token from response.

## Test Protected Endpoint

```bash
curl http://localhost:3001/api/profile \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

Should return user profile.

## Test Admin Endpoint

```bash
curl http://localhost:3001/api/admin/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

Should return users list if you have Admin role.

---

# 🔧 Common Issues

## Issue: "Cannot find module 'primus-identity-validator'"

**Solution:**
```bash
npm install primus-identity-validator --save
```

## Issue: CORS Error in Browser

**Solution:** Add CORS middleware in backend:
```javascript
app.use(cors({
  origin: 'http://localhost:5173',
  credentials: true
}));
```

## Issue: "401 Unauthorized" on all requests

**Solution:**
1. Check token is in localStorage: `localStorage.getItem('auth_token')`
2. Verify `PRIMUS_JWT_SECRET` in backend `.env`
3. Ensure login was successful

## Issue: Token expired

**Solution:** Login again to get a new token. Tokens typically expire after 60 minutes.

---

# 🎯 Next Steps

## Add More Features

### 1. Profile Page
```javascript
// src/pages/ProfilePage.jsx
export const ProfilePage = () => {
  const { user } = useAuth();
  
  return (
    <div>
      <h1>My Profile</h1>
      <p>Email: {user?.email}</p>
      <p>Roles: {user?.roles.join(', ')}</p>
    </div>
  );
};
```

### 2. Conditional UI Rendering
```javascript
// Show/hide based on role
{hasRole('Admin') && <AdminButton />}
{hasRole('Manager') && <ManagerDashboard />}
```

### 3. Add More Protected Routes
```javascript
<Route
  path="/settings"
  element={
    <ProtectedRoute>
      <SettingsPage />
    </ProtectedRoute>
  }
/>
```

### 4. Token Refresh
Implement auto-refresh before token expires (see Advanced section in full docs).

---

# 📚 Resources

- **NPM Package**: https://www.npmjs.com/package/primus-identity-validator
- **Example Apps**: See `examples/nodejs-express/` in repository
- **Full Documentation**: See `INTEGRATION_GUIDE.md`

---

# 🎉 Congratulations!

Your React + Express app is now secured with Primus Identity Validator!

**What you achieved:**
- ✅ Secure JWT authentication
- ✅ Protected API endpoints
- ✅ Role-based access control
- ✅ Complete login/logout flow
- ✅ User session management

**Ready for production?** See deployment guide in main documentation.

Happy coding! 🚀
