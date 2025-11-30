import { useEffect, useState } from 'react';
import axios from 'axios';
import { motion } from 'framer-motion';
import { Shield, Lock, LayoutDashboard, LogIn, CheckCircle, AlertTriangle, Cloud, Server, Mail, Activity, Phone, BarChart3, Cpu, HardDrive, Clock, Wifi } from 'lucide-react';
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
  const [previewType, setPreviewType] = useState('SmsDemo');
  const [previewChannel, setPreviewChannel] = useState('SmsBody');
  const [previewMessage, setPreviewMessage] = useState('Your Primus demo code is 123456');
  const [previewPhone, setPreviewPhone] = useState('+15551234567');
  const [previewName, setPreviewName] = useState('Primus Demo User');
  const [previewEmail, setPreviewEmail] = useState('demo@primus.local');
  const [previewResult, setPreviewResult] = useState<string>('');
  const [previewError, setPreviewError] = useState<any>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [autoLogs, setAutoLogs] = useState(false);
  
  // Telemetry dashboard state
  const [telemetry, setTelemetry] = useState<any>(null);
  const [telemetryError, setTelemetryError] = useState<any>(null);
  const [isLoadingTelemetry, setIsLoadingTelemetry] = useState(false);
  const [autoTelemetry, setAutoTelemetry] = useState(true); // Auto-refresh enabled by default

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

  const previewTemplate = async () => {
    setIsPreviewLoading(true);
    setPreviewError(null);
    setPreviewResult('');
    try {
      const response = await axios.post(`${apiBaseUrl}/notifications/templates/preview`, {
        type: previewType,
        channel: previewChannel,
        message: previewMessage,
        phoneNumber: previewPhone,
        name: previewName,
        email: previewEmail
      });
      setPreviewResult(response.data.content || '');
    } catch (err: any) {
      console.error('Template preview failed', err);
      setPreviewError(err);
    } finally {
      setIsPreviewLoading(false);
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

  const fetchTelemetry = async (isAutoRefresh = false) => {
    // Only show loading spinner on manual refresh, not auto-refresh
    if (!isAutoRefresh) {
      setIsLoadingTelemetry(true);
    }
    setTelemetryError(null);
    try {
      const response = await axios.get(`${apiBaseUrl}/telemetry/summary`);
      setTelemetry(response.data);
    } catch (err: any) {
      console.error('Telemetry fetch failed', err);
      setTelemetryError(err);
      // Don't clear telemetry on auto-refresh errors to avoid flicker
      if (!isAutoRefresh) {
        setTelemetry(null);
      }
    } finally {
      if (!isAutoRefresh) {
        setIsLoadingTelemetry(false);
      }
    }
  };

  useEffect(() => {
    if (!autoLogs) return;
    // Start immediate fetch, then poll every 3s
    fetchLogs();
    const id = setInterval(fetchLogs, 3000);
    return () => clearInterval(id);
  }, [autoLogs]);

  useEffect(() => {
    // Always fetch telemetry on mount
    fetchTelemetry(false);
    if (!autoTelemetry) return;
    // Poll every 5s when auto-refresh is on (silent updates)
    const id = setInterval(() => fetchTelemetry(true), 5000);
    return () => clearInterval(id);
  }, [autoTelemetry]);

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
          <h2><Mail size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Template Preview</h2>
          <p className="subtitle">Render Liquid templates without sending. Defaults: type <code>SmsDemo</code>, channel <code>SmsBody</code>.</p>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '150px' }}
                placeholder="Type (folder)"
                value={previewType}
                onChange={(e) => setPreviewType(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '140px' }}
                placeholder="Channel (file)"
                value={previewChannel}
                onChange={(e) => setPreviewChannel(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '220px' }}
                placeholder="Message"
                value={previewMessage}
                onChange={(e) => setPreviewMessage(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '180px' }}
                placeholder="Phone"
                value={previewPhone}
                onChange={(e) => setPreviewPhone(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '180px' }}
                placeholder="Name"
                value={previewName}
                onChange={(e) => setPreviewName(e.target.value)}
              />
              <input
                style={{ padding: '0.65rem 0.75rem', borderRadius: '10px', border: '1px solid #334155', background: 'rgba(255,255,255,0.03)', color: '#e2e8f0', minWidth: '200px' }}
                placeholder="Email"
                value={previewEmail}
                onChange={(e) => setPreviewEmail(e.target.value)}
              />
              <button className="btn" style={{ width: 'auto' }} onClick={previewTemplate} disabled={isPreviewLoading}>
                {isPreviewLoading ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Preview'}
              </button>
            </div>

            {previewResult && (
              <pre style={{
                marginTop: '0.5rem',
                padding: '0.9rem',
                borderRadius: '10px',
                background: 'rgba(148, 163, 184, 0.08)',
                border: '1px solid rgba(148, 163, 184, 0.2)',
                color: '#e2e8f0',
                maxHeight: '220px',
                overflow: 'auto',
                fontSize: '0.9rem',
                whiteSpace: 'pre-wrap'
              }}>
                {previewResult}
              </pre>
            )}

            {previewError && (
              <div className="status-card error" style={{ background: 'rgba(239,68,68,0.08)', borderColor: 'rgba(239,68,68,0.25)' }}>
                <strong>Error:</strong> {previewError.response?.data?.error || previewError.message}
              </div>
            )}
          </div>
        </div>

        {/* Application Insights Telemetry Dashboard */}
        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><BarChart3 size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> Application Insights Telemetry</h2>
          <p className="subtitle">Live server metrics and Application Insights status.</p>

          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginTop: '1rem', flexWrap: 'wrap' }}>
            <button className="btn" style={{ width: 'auto' }} onClick={fetchTelemetry} disabled={isLoadingTelemetry}>
              {isLoadingTelemetry ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Refresh'}
            </button>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: '#cbd5e1', fontSize: '0.9rem' }}>
              <input
                type="checkbox"
                checked={autoTelemetry}
                onChange={(e) => setAutoTelemetry(e.target.checked)}
              />
              Auto-refresh (5s)
            </label>
            {telemetry?.applicationInsights?.enabled && (
              <a 
                href={telemetry.applicationInsights.portalUrl} 
                target="_blank" 
                rel="noopener noreferrer"
                style={{ color: '#3b82f6', fontSize: '0.9rem', textDecoration: 'underline' }}
              >
                Open Azure Portal →
              </a>
            )}
            {telemetryError && (
              <span style={{ color: '#ef4444', fontSize: '0.9rem' }}>
                {telemetryError.response?.data?.message || telemetryError.message}
              </span>
            )}
          </div>

          {telemetry && (
            <div style={{ marginTop: '1rem', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
              {/* Application Insights Status */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: telemetry.applicationInsights?.enabled ? 'rgba(34,197,94,0.08)' : 'rgba(239,68,68,0.08)',
                border: `1px solid ${telemetry.applicationInsights?.enabled ? 'rgba(34,197,94,0.25)' : 'rgba(239,68,68,0.25)'}`,
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <Wifi size={18} color={telemetry.applicationInsights?.enabled ? '#22c55e' : '#ef4444'} />
                  <strong style={{ color: telemetry.applicationInsights?.enabled ? '#22c55e' : '#ef4444' }}>
                    {telemetry.applicationInsights?.enabled ? 'AI Connected' : 'AI Disabled'}
                  </strong>
                </div>
                {telemetry.applicationInsights?.instrumentationKey && (
                  <div style={{ fontSize: '0.8rem', color: '#94a3b8', transition: 'all 0.3s ease' }}>
                    Key: {telemetry.applicationInsights.instrumentationKey.substring(0, 8)}...
                  </div>
                )}
              </div>

              {/* Server Info */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: 'rgba(59,130,246,0.08)',
                border: '1px solid rgba(59,130,246,0.25)',
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <Server size={18} color="#3b82f6" />
                  <strong style={{ color: '#3b82f6' }}>{telemetry.server?.name}</strong>
                </div>
                <div style={{ fontSize: '0.85rem', color: '#94a3b8', transition: 'all 0.3s ease' }}>
                  PID: {telemetry.runtime?.processId}
                </div>
              </div>

              {/* Uptime */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: 'rgba(168,85,247,0.08)',
                border: '1px solid rgba(168,85,247,0.25)',
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <Clock size={18} color="#a855f7" />
                  <strong style={{ color: '#a855f7' }}>Uptime</strong>
                </div>
                <div style={{ fontSize: '0.85rem', color: '#e2e8f0', transition: 'all 0.3s ease' }}>
                  {telemetry.server?.uptime?.formatted}
                </div>
              </div>

              {/* Memory */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: 'rgba(236,72,153,0.08)',
                border: '1px solid rgba(236,72,153,0.25)',
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <HardDrive size={18} color="#ec4899" />
                  <strong style={{ color: '#ec4899' }}>Memory</strong>
                </div>
                <div style={{ fontSize: '0.85rem', color: '#e2e8f0', transition: 'all 0.3s ease' }}>
                  {telemetry.memory?.workingSetMB} MB working set
                </div>
                <div style={{ fontSize: '0.8rem', color: '#94a3b8', transition: 'all 0.3s ease' }}>
                  GC: {telemetry.memory?.gcTotalMemoryMB} MB
                </div>
              </div>

              {/* Threads */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: 'rgba(245,158,11,0.08)',
                border: '1px solid rgba(245,158,11,0.25)',
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <Cpu size={18} color="#f59e0b" />
                  <strong style={{ color: '#f59e0b' }}>Threads</strong>
                </div>
                <div style={{ fontSize: '0.85rem', color: '#e2e8f0', transition: 'all 0.3s ease' }}>
                  {telemetry.runtime?.threadCount} active
                </div>
              </div>

              {/* Runtime */}
              <div style={{
                padding: '1rem',
                borderRadius: '12px',
                background: 'rgba(20,184,166,0.08)',
                border: '1px solid rgba(20,184,166,0.25)',
                transition: 'all 0.3s ease'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <Activity size={18} color="#14b8a6" />
                  <strong style={{ color: '#14b8a6' }}>Runtime</strong>
                </div>
                <div style={{ fontSize: '0.8rem', color: '#94a3b8', wordBreak: 'break-word', transition: 'all 0.3s ease' }}>
                  {telemetry.runtime?.framework?.replace('.NET ', '.NET\n')}
                </div>
              </div>
            </div>
          )}

          {telemetry?.timestamp && (
            <div style={{ marginTop: '1rem', fontSize: '0.8rem', color: '#64748b', textAlign: 'right' }}>
              Last updated: {new Date(telemetry.timestamp).toLocaleTimeString()}
            </div>
          )}
        </div>

        <div className="card" style={{ maxWidth: '100%', textAlign: 'left', marginTop: '1.5rem' }}>
          <h2><Activity size={20} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '0.5rem' }} /> API Logs</h2>
          <p className="subtitle">Tail of the backend structured log (demo-only endpoint).</p>

          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginTop: '1rem' }}>
            <button className="btn" style={{ width: 'auto' }} onClick={fetchLogs} disabled={isLoadingLogs}>
              {isLoadingLogs ? <div className="loader" style={{ borderColor: '#3b82f6', borderTopColor: 'transparent' }}></div> : 'Fetch latest logs'}
            </button>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: '#cbd5e1', fontSize: '0.9rem' }}>
              <input
                type="checkbox"
                checked={autoLogs}
                onChange={(e) => setAutoLogs(e.target.checked)}
              />
              Live refresh (3s)
            </label>
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
