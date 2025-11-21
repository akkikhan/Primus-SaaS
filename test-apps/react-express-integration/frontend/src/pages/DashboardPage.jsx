import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService } from '../services/api';

/**
 * Dashboard Page - Main page after login
 */
export const DashboardPage = () => {
  const navigate = useNavigate();
  const { user, logout, hasRole } = useAuth();
  
  const [dashboardData, setDashboardData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Fetch dashboard data on mount
  useEffect(() => {
    const fetchData = async () => {
      try {
        console.log('📊 Fetching dashboard data...');
        const data = await apiService.getDashboard();
        setDashboardData(data);
        console.log('✅ Dashboard data loaded');
      } catch (error) {
        console.error('❌ Failed to fetch dashboard:', error);
        setError('Failed to load dashboard data');
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

  if (loading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>⏳</div>
          <div style={{ fontSize: '18px', color: '#666' }}>Loading dashboard...</div>
        </div>
      </div>
    );
  }

  return (
    <div style={{ 
      minHeight: '100vh',
      backgroundColor: '#f5f5f5',
      padding: '20px'
    }}>
      {/* Header */}
      <header style={{
        backgroundColor: 'white',
        padding: '20px 30px',
        borderRadius: '8px',
        marginBottom: '20px',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }}>
        <h1 style={{ margin: 0, color: '#333' }}>📊 Dashboard</h1>
        <button 
          onClick={handleLogout}
          style={{
            padding: '10px 20px',
            backgroundColor: '#dc3545',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: '600'
          }}
        >
          👋 Logout
        </button>
      </header>

      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto' 
      }}>
        {/* User Info Card */}
        <div style={{
          backgroundColor: 'white',
          padding: '25px',
          borderRadius: '8px',
          marginBottom: '20px',
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
        }}>
          <h2 style={{ marginTop: 0, color: '#333' }}>👤 User Information</h2>
          <div style={{ display: 'grid', gap: '10px' }}>
            <div>
              <strong>Email:</strong> {user?.email}
            </div>
            <div>
              <strong>User ID:</strong> {user?.userId}
            </div>
            <div>
              <strong>Name:</strong> {user?.name || 'N/A'}
            </div>
            <div>
              <strong>Roles:</strong>{' '}
              {user?.roles?.map(role => (
                <span 
                  key={role}
                  style={{
                    display: 'inline-block',
                    padding: '4px 12px',
                    marginRight: '8px',
                    backgroundColor: '#007bff',
                    color: 'white',
                    borderRadius: '12px',
                    fontSize: '14px',
                    fontWeight: '600'
                  }}
                >
                  {role}
                </span>
              ))}
            </div>
            <div>
              <strong>Tenant ID:</strong> {user?.tenantId}
            </div>
          </div>
        </div>

        {/* Dashboard Data Card */}
        {dashboardData && (
          <div style={{
            backgroundColor: 'white',
            padding: '25px',
            borderRadius: '8px',
            marginBottom: '20px',
            boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
          }}>
            <h2 style={{ marginTop: 0, color: '#333' }}>📈 Statistics</h2>
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
              gap: '20px',
              marginBottom: '30px'
            }}>
              <div style={{ 
                textAlign: 'center',
                padding: '20px',
                backgroundColor: '#e3f2fd',
                borderRadius: '8px'
              }}>
                <div style={{ fontSize: '36px', fontWeight: 'bold', color: '#1976d2' }}>
                  {dashboardData.data?.stats?.totalUsers || 0}
                </div>
                <div style={{ color: '#666', marginTop: '8px' }}>Total Users</div>
              </div>
              <div style={{ 
                textAlign: 'center',
                padding: '20px',
                backgroundColor: '#e8f5e9',
                borderRadius: '8px'
              }}>
                <div style={{ fontSize: '36px', fontWeight: 'bold', color: '#388e3c' }}>
                  {dashboardData.data?.stats?.activeProjects || 0}
                </div>
                <div style={{ color: '#666', marginTop: '8px' }}>Active Projects</div>
              </div>
              <div style={{ 
                textAlign: 'center',
                padding: '20px',
                backgroundColor: '#fff3e0',
                borderRadius: '8px'
              }}>
                <div style={{ fontSize: '36px', fontWeight: 'bold', color: '#f57c00' }}>
                  {dashboardData.data?.stats?.completedTasks || 0}
                </div>
                <div style={{ color: '#666', marginTop: '8px' }}>Completed Tasks</div>
              </div>
            </div>

            <h3 style={{ color: '#333' }}>🕐 Recent Activity</h3>
            <div style={{ display: 'grid', gap: '10px' }}>
              {dashboardData.data?.recentActivity?.map((activity, index) => (
                <div 
                  key={index}
                  style={{
                    padding: '12px',
                    backgroundColor: '#f8f9fa',
                    borderLeft: '4px solid #007bff',
                    borderRadius: '4px'
                  }}
                >
                  <strong>{activity.action}</strong> - {activity.time}
                </div>
              ))}
            </div>
          </div>
        )}

        {error && (
          <div style={{
            padding: '20px',
            backgroundColor: '#fee',
            border: '1px solid #fcc',
            borderRadius: '8px',
            color: '#c33'
          }}>
            ❌ {error}
          </div>
        )}

        {/* Action Buttons */}
        <div style={{
          backgroundColor: 'white',
          padding: '25px',
          borderRadius: '8px',
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
        }}>
          <h2 style={{ marginTop: 0, color: '#333' }}>🚀 Quick Actions</h2>
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            {hasRole('Admin') && (
              <>
                <button 
                  onClick={() => navigate('/admin/users')}
                  style={{
                    padding: '12px 24px',
                    backgroundColor: '#dc3545',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontWeight: '600',
                    fontSize: '14px'
                  }}
                >
                  👥 Manage Users (Admin)
                </button>
                <button 
                  onClick={() => navigate('/admin/settings')}
                  style={{
                    padding: '12px 24px',
                    backgroundColor: '#6c757d',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontWeight: '600',
                    fontSize: '14px'
                  }}
                >
                  ⚙️ Settings (Admin)
                </button>
              </>
            )}

            {(hasRole('Manager') || hasRole('Admin')) && (
              <button 
                onClick={() => navigate('/reports')}
                style={{
                  padding: '12px 24px',
                  backgroundColor: '#28a745',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontWeight: '600',
                  fontSize: '14px'
                }}
              >
                📊 View Reports
              </button>
            )}

            <button 
              onClick={() => navigate('/profile')}
              style={{
                padding: '12px 24px',
                backgroundColor: '#007bff',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontWeight: '600',
                fontSize: '14px'
              }}
            >
              👤 My Profile
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
