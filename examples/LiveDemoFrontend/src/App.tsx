import { useState } from 'react';
import axios from 'axios';
import { motion } from 'framer-motion';
import { Shield, Lock, LayoutDashboard, LogIn, CheckCircle, AlertTriangle, Cloud, Server } from 'lucide-react';
import './App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [provider, setProvider] = useState<string>('');
  const [token, setToken] = useState<string>('');
  const [apiData, setApiData] = useState<any>(null);
  const [error, setError] = useState<any>(null);

  const apiBaseUrl = 'https://localhost:7287'; // Backend https port from launchSettings.json

  const handleLogin = async (selectedProvider: string) => {
    setIsLoading(true);
    setProvider(selectedProvider);
    setError(null);

    try {
      let response;
      if (selectedProvider === 'Auth0') {
        response = await axios.post(`${apiBaseUrl}/auth/auth0`);
        setToken(response.data.access_token);
      } else {
        response = await axios.post(`${apiBaseUrl}/auth/azure`);
        setToken(response.data.access_token);
      }
      setIsLoggedIn(true);
    } catch (err: any) {
      console.error(err);
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);

    if (!token) {
      setIsLoading(false);
      setError(new Error('Missing access token. Please log in again.'));
      return;
    }

    try {
      const response = await axios.get(`${apiBaseUrl}/weatherforecast`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });
      setApiData(response.data);
      setError(null);
    } catch (err: any) {
      console.error(err);
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  if (!isLoggedIn) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="card"
      >
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '1rem' }}>
          <Shield size={48} color="#3b82f6" />
        </div>
        <h1 className="title">Primus SaaS</h1>
        <p className="subtitle">Enterprise Identity & Security Demo</p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <button
            className="btn"
            onClick={() => handleLogin('Auth0')}
            disabled={isLoading}
            style={{ backgroundColor: '#eb5424' }}
          >
            {isLoading && provider === 'Auth0' ? <div className="loader"></div> : <><Shield size={20} /> Sign in with Auth0</>}
          </button>

          <button
            className="btn"
            onClick={() => handleLogin('Azure')}
            disabled={isLoading}
            style={{ backgroundColor: '#0078d4' }}
          >
            {isLoading && provider === 'Azure' ? <div className="loader"></div> : <><Cloud size={20} /> Sign in with Azure AD</>}
          </button>
        </div>

        {error && (
          <div style={{ marginTop: '1rem', color: '#ef4444', fontSize: '0.9rem' }}>
            Error: {error.response?.data?.error || error.message}
          </div>
        )}

        <p style={{ marginTop: '1rem', fontSize: '0.8rem', color: '#64748b' }}>
          Powered by PrimusSaaS.Identity.Validator
        </p>
      </motion.div>
    );
  }

  return (
    <div className="container">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        className="dashboard"
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <LayoutDashboard size={32} color="#3b82f6" />
            <h1 className="title" style={{ margin: 0, fontSize: '1.5rem' }}>Dashboard</h1>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <span style={{ color: '#94a3b8', fontSize: '0.9rem' }}>Logged in via <strong>{provider}</strong></span>
            <button className="btn" style={{ width: 'auto', backgroundColor: '#334155' }} onClick={() => { setIsLoggedIn(false); setApiData(null); setError(null); }}>
              Sign Out
            </button>
          </div>
        </div>

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left' }}>
          <h2><Lock size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Secure Data Access</h2>
          <p className="subtitle">Attempting to fetch sensitive data from <code>/weatherforecast</code>...</p>

          {!apiData && !isLoading && !error && (
            <div style={{ textAlign: 'center', padding: '2rem' }}>
              <p>You have a valid <strong>{provider}</strong> token.</p>
              <button className="btn" style={{ maxWidth: '200px', margin: '0 auto' }} onClick={fetchData}>
                <Server size={20} /> Fetch Secure Data
              </button>
            </div>
          )}

          {isLoading && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1rem' }}>
              <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div>
              <span>Verifying Identity Token...</span>
            </div>
          )}

          {!isLoading && error && (
            <motion.div
              initial={{ x: -10 }}
              animate={{ x: 0 }}
              className="status-card error"
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem', color: '#ef4444', fontWeight: 'bold' }}>
                <AlertTriangle size={24} />
                Access Denied ({error.response?.status || 'Error'})
              </div>
              <p>
                The backend rejected the request.
              </p>
              <div className="code-block">
                {JSON.stringify(error.response?.data || error.message, null, 2)}
              </div>
            </motion.div>
          )}

          {!isLoading && apiData && (
            <motion.div
              initial={{ scale: 0.9 }}
              animate={{ scale: 1 }}
              className="status-card success"
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem', color: '#22c55e', fontWeight: 'bold' }}>
                <CheckCircle size={24} />
                Access Granted
              </div>
              <p>Identity verified by <strong>PrimusSaaS</strong>. Data retrieved successfully.</p>
              <div className="code-block">
                {JSON.stringify(apiData, null, 2)}
              </div>
            </motion.div>
          )}

        </div>
      </motion.div>
    </div>
  );
}

export default App;
