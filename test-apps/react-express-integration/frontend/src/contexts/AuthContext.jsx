import { createContext, useContext, useState, useEffect } from 'react';
import { apiService } from '../services/api';

const AuthContext = createContext(null);

/**
 * Auth Provider - Manages global authentication state
 * Wraps entire app to provide auth state to all components
 */
export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check if user is already logged in on page load
  useEffect(() => {
    const checkAuth = async () => {
      // If token exists in localStorage
      if (apiService.isAuthenticated()) {
        try {
          console.log('🔍 Checking existing token...');
          
          // Fetch user profile to validate token
          const profileData = await apiService.getProfile();
          setUser(profileData.user);
          
          console.log('✅ Token valid - user loaded:', profileData.user.email);
        } catch (error) {
          console.error('❌ Token invalid - clearing auth');
          apiService.clearAuth();
        }
      }
      setIsLoading(false);
    };

    checkAuth();
  }, []);

  /**
   * Login user
   */
  const login = async (email, password) => {
    console.log('🔐 AuthContext: Logging in...');
    
    // 1. Call API service to login
    await apiService.login(email, password);
    
    // 2. Fetch user profile
    const profileData = await apiService.getProfile();
    setUser(profileData.user);
    
    console.log('✅ AuthContext: User logged in:', profileData.user.email);
  };

  /**
   * Logout user
   */
  const logout = () => {
    console.log('👋 AuthContext: Logging out');
    apiService.logout();
    setUser(null);
  };

  /**
   * Check if user has specific role
   */
  const hasRole = (role) => {
    return user?.roles?.includes(role) || false;
  };

  /**
   * Check if user has any of the specified roles
   */
  const hasAnyRole = (...roles) => {
    return roles.some(role => user?.roles?.includes(role));
  };

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user,
      isLoading,
      login,
      logout,
      hasRole,
      hasAnyRole,
    }}>
      {children}
    </AuthContext.Provider>
  );
};

/**
 * Hook to use auth context
 * Usage: const { user, login, logout } = useAuth();
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
