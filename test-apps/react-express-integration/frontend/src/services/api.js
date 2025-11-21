import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:3001';
const PORTAL_URL = import.meta.env.VITE_PRIMUS_PORTAL_URL || 'http://localhost:5000';

/**
 * API Service - Handles all HTTP requests to backend
 * Automatically manages JWT tokens and authentication
 */
class ApiService {
  constructor() {
    // Create axios instance with base URL
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Load token from localStorage on init
    this.token = localStorage.getItem('auth_token');
    if (this.token) {
      this.setAuthToken(this.token);
    }

    // Intercept responses to handle 401 errors
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          console.warn('🔐 Token expired or invalid - redirecting to login');
          this.clearAuth();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  /**
   * Set authentication token
   * Adds token to all future requests
   */
  setAuthToken(token) {
    this.token = token;
    this.api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    localStorage.setItem('auth_token', token);
    console.log('✅ Token saved to localStorage');
  }

  /**
   * Clear authentication
   * Removes token from localStorage and headers
   */
  clearAuth() {
    this.token = null;
    delete this.api.defaults.headers.common['Authorization'];
    localStorage.removeItem('auth_token');
    console.log('🗑️  Token removed from localStorage');
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated() {
    return !!this.token;
  }

  // ===== AUTHENTICATION =====
  
  /**
   * Login user
   * Calls Primus Portal /api/auth/login
   */
  async login(email, password) {
    console.log('🔐 Logging in:', email);
    
    const response = await axios.post(`${PORTAL_URL}/api/auth/login`, {
      email,
      password
    });
    
    const { token, user } = response.data;
    this.setAuthToken(token);
    
    console.log('✅ Login successful:', user.email);
    return response.data;
  }

  /**
   * Logout user
   * Clears token
   */
  logout() {
    console.log('👋 Logging out');
    this.clearAuth();
  }

  // ===== PUBLIC ENDPOINTS =====

  async getHealth() {
    const response = await this.api.get('/api/health');
    return response.data;
  }

  async getPublicData() {
    const response = await this.api.get('/api/public-data');
    return response.data;
  }

  // ===== PROTECTED ENDPOINTS =====

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

// Export singleton instance
export const apiService = new ApiService();
