import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

/**
 * ProtectedRoute Component
 * Wraps routes that require authentication
 * 
 * Usage:
 * <Route path="/dashboard" element={
 *   <ProtectedRoute>
 *     <DashboardPage />
 *   </ProtectedRoute>
 * } />
 * 
 * With role requirement:
 * <Route path="/admin" element={
 *   <ProtectedRoute requiredRole="Admin">
 *     <AdminPage />
 *   </ProtectedRoute>
 * } />
 */
export const ProtectedRoute = ({ children, requiredRole }) => {
  const { isAuthenticated, isLoading, hasRole } = useAuth();

  // Show loading while checking auth
  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>⏳</div>
          <div style={{ fontSize: '18px', color: '#666' }}>Loading...</div>
        </div>
      </div>
    );
  }

  // Not authenticated - redirect to login
  if (!isAuthenticated) {
    console.warn('🔒 Protected route - redirecting to login');
    return <Navigate to="/login" replace />;
  }

  // Authenticated but missing required role - redirect to unauthorized
  if (requiredRole && !hasRole(requiredRole)) {
    console.warn(`🔒 Missing required role: ${requiredRole}`);
    return <Navigate to="/unauthorized" replace />;
  }

  // All checks passed - render the protected component
  return children;
};
