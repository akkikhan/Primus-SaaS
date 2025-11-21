import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

/**
 * Main App Component
 * Sets up routing and authentication
 */
function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected Routes */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            }
          />

          <Route
            path="/profile"
            element={
              <ProtectedRoute>
                <div style={{ padding: '30px' }}>
                  <h1>👤 My Profile</h1>
                  <p>Profile page - requires login</p>
                </div>
              </ProtectedRoute>
            }
          />

          {/* Admin Only Routes */}
          <Route
            path="/admin/users"
            element={
              <ProtectedRoute requiredRole="Admin">
                <div style={{ padding: '30px' }}>
                  <h1>👥 User Management</h1>
                  <p>Admin only page - requires Admin role</p>
                </div>
              </ProtectedRoute>
            }
          />

          <Route
            path="/admin/settings"
            element={
              <ProtectedRoute requiredRole="Admin">
                <div style={{ padding: '30px' }}>
                  <h1>⚙️ Settings</h1>
                  <p>Admin only page - requires Admin role</p>
                </div>
              </ProtectedRoute>
            }
          />

          {/* Manager/Admin Routes */}
          <Route
            path="/reports"
            element={
              <ProtectedRoute>
                <div style={{ padding: '30px' }}>
                  <h1>📊 Reports</h1>
                  <p>Reports page - requires Manager or Admin role</p>
                </div>
              </ProtectedRoute>
            }
          />

          {/* Unauthorized Page */}
          <Route 
            path="/unauthorized" 
            element={
              <div style={{ 
                padding: '30px', 
                textAlign: 'center',
                minHeight: '100vh',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                backgroundColor: '#f5f5f5'
              }}>
                <div>
                  <div style={{ fontSize: '72px', marginBottom: '20px' }}>⛔</div>
                  <h1 style={{ color: '#333' }}>Unauthorized</h1>
                  <p style={{ color: '#666', fontSize: '18px' }}>
                    You don't have permission to access this page
                  </p>
                  <a href="/dashboard" style={{
                    display: 'inline-block',
                    marginTop: '20px',
                    padding: '12px 24px',
                    backgroundColor: '#007bff',
                    color: 'white',
                    textDecoration: 'none',
                    borderRadius: '4px',
                    fontWeight: '600'
                  }}>
                    Go to Dashboard
                  </a>
                </div>
              </div>
            } 
          />

          {/* Default Route - Redirect to Dashboard */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />

          {/* 404 Not Found */}
          <Route 
            path="*" 
            element={
              <div style={{ 
                padding: '30px', 
                textAlign: 'center',
                minHeight: '100vh',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                backgroundColor: '#f5f5f5'
              }}>
                <div>
                  <div style={{ fontSize: '72px', marginBottom: '20px' }}>🔍</div>
                  <h1 style={{ color: '#333' }}>404 - Page Not Found</h1>
                  <p style={{ color: '#666', fontSize: '18px' }}>
                    The page you're looking for doesn't exist
                  </p>
                  <a href="/dashboard" style={{
                    display: 'inline-block',
                    marginTop: '20px',
                    padding: '12px 24px',
                    backgroundColor: '#007bff',
                    color: 'white',
                    textDecoration: 'none',
                    borderRadius: '4px',
                    fontWeight: '600'
                  }}>
                    Go to Dashboard
                  </a>
                </div>
              </div>
            } 
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
