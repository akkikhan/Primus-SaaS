import { useState } from 'react';
import axios from 'axios';
import { motion } from 'framer-motion';
import { Shield, Lock, LayoutDashboard, LogIn, CheckCircle, AlertTriangle, Cloud, Server, Mail, Activity, Phone } from 'lucide-react';
import './App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [provider, setProvider] = useState<string>('');
  const [token, setToken] = useState<string>('');
  const [localEmail, setLocalEmail] = useState('demo@primus.local');
  const [localPassword, setLocalPassword] = useState('PrimusDemo123!');
  const [apiData, setApiData] = useState<any>(null);
  const [error, setError] = useState<any>(null);
  const [health, setHealth] = useState<any>(null);
  const [healthError, setHealthError] = useState<any>(null);
  const [isHealthLoading, setIsHealthLoading] = useState(false);
  const [notificationEmail, setNotificationEmail] = useState('akki@primussoft.com');
  const [notificationName, setNotificationName] = useState('Akki');
  const [notificationResult, setNotificationResult] = useState<any>(null);
  const [notificationError, setNotificationError] = useState<any>(null);
  const [isSendingNotification, setIsSendingNotification] = useState(false);
  const [smsPhone, setSmsPhone] = useState('+15551234567');
  const [smsMessage, setSmsMessage] = useState('Your Primus demo code is 123456');
  const [smsResult, setSmsResult] = useState<any>(null);
  const [smsError, setSmsError] = useState<any>(null);
  const [isSendingSms, setIsSendingSms] = useState(false);
  const [logs, setLogs] = useState<string>('');
  const [logsMeta, setLogsMeta] = useState<{ file?: string; size?: number } | null>(null);
  const [logsError, setLogsError] = useState<any>(null);
  const [isLoadingLogs, setIsLoadingLogs] = useState(false);

  // Prefer env override; fall back to http dev port (5221) to avoid HTTPS cert hassles
  const apiBaseUrl =
    (import.meta.env.VITE_API_BASE_URL && import.meta.env.VITE_API_BASE_URL.trim()) ||
    'http://localhost:5221';

  const handleLogin = async (selectedProvider: string) => {
    setIsLoading(true);
    setProvider(selectedProvider);
    setError(null);

    try {
      let response;
      if (selectedProvider === 'Auth0') {
        console.log('Auth0 login: requesting token');
        response = await axios.post(`${apiBaseUrl}/auth/auth0`);
        setToken(response.data.access_token);
      } else if (selectedProvider === 'Azure') {
        console.log('Azure login: requesting token');
        response = await axios.post(`${apiBaseUrl}/auth/azure`);
        setToken(response.data.access_token);
      } else {
        if (!localEmail || !localPassword) {
          setIsLoading(false);
          setError(new Error('Email and password are required for Local JWT login.'));
          return;
        }
        console.log('Local JWT login: requesting token');
        response = await axios.post(`${apiBaseUrl}/auth/local`, {
          email: localEmail,
          password: localPassword
        });
        setToken(response.data.access_token);
      }
      console.log('Token received:', selectedProvider, response.data);
      setIsLoggedIn(true);
    } catch (err: any) {
      console.error('Login failed', err);
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
      console.log('Calling /weatherforecast with token to', apiBaseUrl);
      const response = await axios.get(`${apiBaseUrl}/weatherforecast`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });
      console.log('Weatherforecast response', response.data);
      setApiData(response.data);
      setError(null);
    } catch (err: any) {
      console.error('Weatherforecast failed', err);
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  const checkNotificationHealth = async () => {
    setIsHealthLoading(true);
    setHealthError(null);
    try {
      console.log('Checking notification health');
      const response = await axios.get(`${apiBaseUrl}/notifications/health`);
      console.log('Notification health response', response.data);
      setHealth(response.data);
    } catch (err: any) {
      console.error('Notification health failed', err);
      setHealthError(err);
    } finally {
      setIsHealthLoading(false);
    }
  };

  const sendWelcomeEmail = async () => {
    setIsSendingNotification(true);
    setNotificationError(null);
    setNotificationResult(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/notifications/welcome`, {
        email: notificationEmail,
        name: notificationName
      });
      console.log('Welcome email response', response.data);
      setNotificationResult(response.data);
    } catch (err: any) {
      console.error('Welcome email failed', err);
      setNotificationError(err);
    } finally {
      setIsSendingNotification(false);
    }
  };

  const sendSms = async () => {
    setIsSendingSms(true);
    setSmsError(null);
    setSmsResult(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/notifications/sms`, {
        phoneNumber: smsPhone,
        message: smsMessage
      });
      console.log('SMS response', response.data);
      setSmsResult(response.data);
    } catch (err: any) {
      console.error('SMS failed', err);
      setSmsError(err);
    } finally {
      setIsSendingSms(false);
    }
  };

  const fetchLogs = async () => {
    setIsLoadingLogs(true);
    setLogsError(null);
    try {
      console.log('Fetching recent logs');
      const response = await axios.get(`${apiBaseUrl}/logs/recent`);
      console.log('Logs response meta', response.data?.file, response.data?.size);
      setLogs(response.data.tail || '');
      setLogsMeta({ file: response.data.file, size: response.data.size });
    } catch (err: any) {
      console.error('Logs fetch failed', err);
      setLogsError(err);
      setLogs('');
      setLogsMeta(null);
    } finally {
      setIsLoadingLogs(false);
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

        <div style={{ marginTop: '1.5rem', padding: '1rem', borderRadius: '12px', background: 'rgba(51,65,85,0.4)', border: '1px solid #334155', textAlign: 'left' }}>
          <p className="subtitle" style={{ marginBottom: '0.75rem' }}>
            Local JWT (shared secret) — exercise the Jwt issuer path without relying on Azure or Auth0.
          </p>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '220px' }}
                placeholder="Email"
                value={localEmail}
                onChange={(e) => setLocalEmail(e.target.value)}
              />
              <input
                type="password"
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '200px' }}
                placeholder="Password"
                value={localPassword}
                onChange={(e) => setLocalPassword(e.target.value)}
              />
              <button
                className="btn"
                style={{ width: 'auto', backgroundColor: '#0ea5e9' }}
                onClick={() => handleLogin('LocalJwt')}
                disabled={isLoading}
              >
                {isLoading && provider === 'LocalJwt' ? <div className="loader"></div> : <><LogIn size={20} /> Sign in with Local JWT</>}
              </button>
            </div>
            <span style={{ color: '#94a3b8', fontSize: '0.85rem' }}>
              Default demo creds: demo@primus.local / PrimusDemo123! (override via appsettings or environment vars)
            </span>
          </div>
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
            <button className="btn" style={{ width: 'auto', backgroundColor: '#334155' }} onClick={() => { setIsLoggedIn(false); setApiData(null); setError(null); setToken(''); setProvider(''); }}>
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

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><Activity size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Notification Health</h2>
          <p className="subtitle">Check the configured channels (SMTP/Logger).</p>
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginTop: '1rem' }}>
            <button className="btn" style={{ width: 'auto' }} onClick={checkNotificationHealth} disabled={isHealthLoading}>
              {isHealthLoading ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Run health check'}
            </button>
            {health && (
              <span style={{ color: '#22c55e', fontSize: '0.9rem' }}>
                Email: {health.channels?.email} | SMS: {health.channels?.sms} | Logger: {health.channels?.logger}
              </span>
            )}
            {healthError && (
              <span style={{ color: '#ef4444', fontSize: '0.9rem' }}>
                {healthError.response?.status || ''} {healthError.message}
              </span>
            )}
          </div>
        </div>

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><Mail size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Send Welcome Email</h2>
          <p className="subtitle">Uses /notifications/welcome with the configured SMTP (Gmail) and logger fallback.</p>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '240px' }}
                placeholder="Recipient email"
                value={notificationEmail}
                onChange={(e) => setNotificationEmail(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '180px' }}
                placeholder="Recipient name"
                value={notificationName}
                onChange={(e) => setNotificationName(e.target.value)}
              />
              <button className="btn" style={{ width: 'auto' }} onClick={sendWelcomeEmail} disabled={isSendingNotification}>
                {isSendingNotification ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Send test email'}
              </button>
            </div>

            {notificationResult && (
              <div className="status-card success" style={{ background: 'rgba(34,197,94,0.08)', borderColor: 'rgba(34,197,94,0.25)' }}>
                <strong>Sent via:</strong> {notificationResult.channel || 'Logger'} | queued: {String(notificationResult.queued)}
              </div>
            )}

            {notificationError && (
              <div className="status-card error" style={{ background: 'rgba(239,68,68,0.08)', borderColor: 'rgba(239,68,68,0.25)' }}>
                <strong>Error:</strong> {notificationError.response?.data?.detail || notificationError.message}
              </div>
            )}
          </div>
        </div>

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><Phone size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Send SMS</h2>
          <p className="subtitle">Uses /notifications/sms with Twilio if configured; otherwise falls back to logger.</p>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '200px' }}
                placeholder="Recipient phone (E.164)"
                value={smsPhone}
                onChange={(e) => setSmsPhone(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '260px' }}
                placeholder="Message"
                value={smsMessage}
                onChange={(e) => setSmsMessage(e.target.value)}
              />
              <button className="btn" style={{ width: 'auto' }} onClick={sendSms} disabled={isSendingSms}>
                {isSendingSms ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Send SMS'}
              </button>
            </div>

            {smsResult && (
              <div className="status-card success" style={{ background: 'rgba(34,197,94,0.08)', borderColor: 'rgba(34,197,94,0.25)' }}>
                <strong>Sent via:</strong> {smsResult.channel || 'Logger'} | queued: {String(smsResult.queued)}
              </div>
            )}

            {smsError && (
              <div className="status-card error" style={{ background: 'rgba(239,68,68,0.08)', borderColor: 'rgba(239,68,68,0.25)' }}>
                <strong>Error:</strong> {smsError.response?.data?.detail || smsError.message}
              </div>
            )}
          </div>
        </div>

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><Activity size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> API Logs</h2>
          <p className="subtitle">Tail of the backend structured log (demo-only endpoint).</p>

          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginTop: '1rem' }}>
            <button className="btn" style={{ width: 'auto' }} onClick={fetchLogs} disabled={isLoadingLogs}>
              {isLoadingLogs ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Fetch latest logs'}
            </button>
            {logsMeta?.file && (
              <span style={{ color: '#22c55e', fontSize: '0.9rem' }}>
                {logsMeta.file} ({logsMeta.size} bytes)
              </span>
            )}
            {logsError && (
              <span style={{ color: '#ef4444', fontSize: '0.9rem' }}>
                {logsError.response?.data?.message || logsError.message}
              </span>
            )}
          </div>

          {logs && (
            <pre style={{
              marginTop: '1rem',
              padding: '1rem',
              borderRadius: '12px',
              background: 'rgba(148, 163, 184, 0.08)',
              border: '1px solid rgba(148, 163, 184, 0.2)',
              color: '#e2e8f0',
              maxHeight: '320px',
              overflow: 'auto',
              fontSize: '0.85rem'
            }}>
{logs}
            </pre>
          )}
        </div>
      </motion.div>
    </div>
  );
}

export default App;
