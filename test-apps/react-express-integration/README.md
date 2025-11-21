# 🚀 Primus React + Express Integration Example

Complete working example of React frontend + Express backend integrated with Primus Identity Validator.

## 📂 Project Structure

```
react-express-integration/
├── backend/
│   ├── server.js          # Express server with Primus auth
│   ├── package.json       # Backend dependencies
│   └── .env              # Backend configuration
└── frontend/
    ├── src/
    │   ├── services/
    │   │   └── api.js    # API service (axios + token management)
    │   ├── contexts/
    │   │   └── AuthContext.jsx  # Global auth state
    │   ├── components/
    │   │   └── ProtectedRoute.jsx  # Route protection
    │   ├── pages/
    │   │   ├── LoginPage.jsx     # Login form
    │   │   └── DashboardPage.jsx # Main dashboard
    │   ├── App.jsx       # Router and routes
    │   └── main.jsx      # React entry point
    ├── index.html        # HTML template
    ├── vite.config.js    # Vite configuration
    ├── package.json      # Frontend dependencies
    └── .env             # Frontend configuration
```

## 🎯 How It Works

### Authentication Flow

1. **User visits app** → Redirected to `/login`
2. **User enters credentials** → API calls Primus Portal
3. **Portal returns JWT token** → Saved to localStorage
4. **Token attached to requests** → All API calls include `Authorization: Bearer {token}`
5. **Backend validates token** → Primus SDK validates with Portal
6. **User info available** → Backend sends user data in response

### Key Integration Points

#### Backend (`server.js`)
```javascript
import { primusIdentityMiddleware, requireRoles } from 'primus-identity-validator';

const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL,
  clientId: process.env.PRIMUS_CLIENT_ID,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET,
  mode: 'Local',
  jwtSecret: process.env.PRIMUS_JWT_SECRET,
});

// Protected route
app.get('/api/profile', primusAuth, (req, res) => {
  res.json({ user: req.user });
});

// Admin-only route
app.get('/api/admin/users', primusAuth, requireRoles('Admin'), (req, res) => {
  // Only admins can access
});
```

#### Frontend (`api.js`)
```javascript
// Login
await apiService.login(email, password);

// Token automatically added to all requests
await apiService.getProfile();
```

#### React Context (`AuthContext.jsx`)
```javascript
const { user, login, logout, hasRole } = useAuth();

// Check roles
if (hasRole('Admin')) {
  // Show admin features
}
```

#### Protected Routes (`App.jsx`)
```javascript
<Route path="/dashboard" element={
  <ProtectedRoute>
    <DashboardPage />
  </ProtectedRoute>
} />

<Route path="/admin" element={
  <ProtectedRoute requiredRole="Admin">
    <AdminPage />
  </ProtectedRoute>
} />
```

## 🔧 Setup Instructions

### Step 1: Configure Backend

1. Navigate to backend directory:
   ```bash
   cd backend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Update `.env` file:
   ```env
   PRIMUS_CLIENT_ID=your-actual-client-id
   PRIMUS_CLIENT_SECRET=your-actual-client-secret
   ```

4. Start backend:
   ```bash
   npm run dev
   ```

   Should see:
   ```
   🚀 Primus Integration Backend Started!
   📡 Server: http://localhost:3001
   🔐 Auth Mode: Local
   ```

### Step 2: Configure Frontend

1. Navigate to frontend directory:
   ```bash
   cd ../frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start frontend:
   ```bash
   npm run dev
   ```

   Should see:
   ```
   VITE ready in 500ms
   ➜  Local:   http://localhost:5173/
   ```

### Step 3: Test

1. Open browser: `http://localhost:5173`
2. Click "Fill Test Credentials"
3. Click "Login"
4. Should see dashboard with user info

## 🧪 Test Credentials

- **Email**: `admin@primussaas.com`
- **Password**: `Admin123!`

## 📍 Available Endpoints

### Backend Endpoints

#### Public (no auth needed):
- `GET /api/health` - Server health check
- `GET /api/public-data` - Public data

#### Protected (login required):
- `GET /api/profile` - Current user profile
- `GET /api/dashboard` - Dashboard data

#### Admin only:
- `GET /api/admin/users` - User management
- `GET /api/admin/settings` - System settings

#### Manager/Admin:
- `GET /api/reports` - Reports data

### Frontend Routes

- `/login` - Login page (public)
- `/dashboard` - Main dashboard (protected)
- `/profile` - User profile (protected)
- `/admin/users` - User management (Admin only)
- `/admin/settings` - Settings (Admin only)
- `/reports` - Reports (Manager/Admin only)

## 🔍 Testing the Integration

### Test 1: Public Endpoint (No Auth)
```bash
curl http://localhost:3001/api/health
```
✅ Should return: `{"status":"healthy"}`

### Test 2: Protected Endpoint (Without Auth)
```bash
curl http://localhost:3001/api/profile
```
❌ Should return: `{"error":"Unauthorized"}`

### Test 3: Login and Get Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@primussaas.com","password":"Admin123!"}'
```
✅ Should return: `{"token":"eyJhbGc..."}`

### Test 4: Protected Endpoint (With Auth)
```bash
curl http://localhost:3001/api/profile \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```
✅ Should return: `{"user":{"email":"admin@primussaas.com",...}}`

## 📊 What Each File Does

### Backend Files

**`server.js`**
- Express server setup
- Primus middleware configuration
- Route definitions (public, protected, admin)
- Error handling

**`.env`**
- Server port
- Primus Portal URL
- Client ID and Secret
- JWT secret for token validation

### Frontend Files

**`services/api.js`**
- All API calls to backend
- Token storage in localStorage
- Auto-attach token to requests
- Auto-redirect on 401 errors

**`contexts/AuthContext.jsx`**
- Global authentication state
- Login/logout functions
- User info (email, roles)
- Role checking helper functions

**`components/ProtectedRoute.jsx`**
- Wraps routes that need authentication
- Redirects to login if not authenticated
- Checks role requirements

**`pages/LoginPage.jsx`**
- Login form
- Calls AuthContext.login()
- Redirects to dashboard on success

**`pages/DashboardPage.jsx`**
- Main page after login
- Shows user info
- Fetches dashboard data
- Role-based action buttons

**`App.jsx`**
- React Router setup
- Route definitions
- Wraps app with AuthProvider

## 🔐 Security Features

✅ **JWT Token Validation** - Backend validates every token
✅ **Role-Based Access** - Different permissions for Admin/Manager/User
✅ **Auto Token Expiry** - Frontend auto-redirects when token expires
✅ **CORS Protection** - Only allowed frontend can call backend
✅ **Secure Storage** - Tokens stored in localStorage (httpOnly recommended for production)

## 🚀 Production Checklist

Before deploying:

- [ ] Change `PRIMUS_JWT_SECRET` to strong random string
- [ ] Update `PRIMUS_PORTAL_URL` to production URL
- [ ] Use environment variables (don't commit `.env`)
- [ ] Enable HTTPS
- [ ] Add rate limiting
- [ ] Add request logging
- [ ] Use httpOnly cookies instead of localStorage
- [ ] Add refresh token rotation
- [ ] Enable CORS only for production frontend domain

## 💡 Next Steps

1. **Add More Pages** - Create profile edit, settings pages
2. **Add Refresh Tokens** - Implement token refresh before expiry
3. **Add Loading States** - Better UX with spinners
4. **Add Error Boundaries** - Catch React errors
5. **Add Form Validation** - Client-side validation
6. **Add Toast Notifications** - Success/error messages

## 📚 Resources

- **Primus Identity Validator**: https://www.npmjs.com/package/primus-identity-validator
- **React Router**: https://reactrouter.com/
- **Axios**: https://axios-http.com/
- **Vite**: https://vitejs.dev/

---

**Questions?** Check the main `INTEGRATION_GUIDE.md` in the repository root.
