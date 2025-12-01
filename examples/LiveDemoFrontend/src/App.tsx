import { useEffect, useState } from 'react';
import axios from 'axios';
import { motion } from 'framer-motion';
import {
  Shield,
  Lock,
  LayoutDashboard,
  LogIn,
  LogOut,
  CheckCircle,
  CheckCircle2,
  AlertTriangle,
  Cloud,
  Server,
  Mail,
  Activity,
  Phone,
  BarChart3,
  Cpu,
  HardDrive,
  Clock,
  Wifi,
  Send,
  RefreshCw,
  FileText,
  Download,
  Zap
} from 'lucide-react';
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
  const [editorContent, setEditorContent] = useState('');
  const [isLoadingTemplate, setIsLoadingTemplate] = useState(false);
  const [isSavingTemplate, setIsSavingTemplate] = useState(false);
  const [templateLoadError, setTemplateLoadError] = useState<any>(null);
  const [templateSaveError, setTemplateSaveError] = useState<any>(null);
  const [templateSaveSuccess, setTemplateSaveSuccess] = useState<string>('');
  const [autoLogs, setAutoLogs] = useState(false);
  const [logFilterLevel, setLogFilterLevel] = useState<'All' | 'Info' | 'Warn' | 'Error'>('All');
  const [logSearch, setLogSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'overview' | 'notifications' | 'templates' | 'insights' | 'logs' | 'goldenpath'>('goldenpath');
  const typeOptions = ['SmsDemo', 'Welcome', 'EmailDemo', 'PasswordReset'];
  const channelOptions = ['SmsBody', 'EmailBody', 'SmsPreview', 'EmailPreview', 'EmailSubject'];
  
  // Telemetry dashboard state
  const [telemetry, setTelemetry] = useState<any>(null);
  const [telemetryError, setTelemetryError] = useState<any>(null);
  const [isLoadingTelemetry, setIsLoadingTelemetry] = useState(false);
  const [autoTelemetry, setAutoTelemetry] = useState(true); // Auto-refresh enabled by default

  // Golden Path state
  const [whoamiResult, setWhoamiResult] = useState<any>(null);
  const [whoamiError, setWhoamiError] = useState<any>(null);
  const [isLoadingWhoami, setIsLoadingWhoami] = useState(false);
  const [logTestResult, setLogTestResult] = useState<any>(null);
  const [logTestError, setLogTestError] = useState<any>(null);
  const [isLoadingLogTest, setIsLoadingLogTest] = useState(false);
  const [logTestMessage, setLogTestMessage] = useState('Hello from Golden Path!');
  const [logTestLevel, setLogTestLevel] = useState('Information');
  const [notifTestResult, setNotifTestResult] = useState<any>(null);
  const [notifTestError, setNotifTestError] = useState<any>(null);
  const [isLoadingNotifTest, setIsLoadingNotifTest] = useState(false);
  const [notifTestEmail, setNotifTestEmail] = useState('demo@primus.local');
  const [notifTestName, setNotifTestName] = useState('Demo User');
  const [notifTestCode, setNotifTestCode] = useState('123456');

  // Document Renderer state
  const [docTitle, setDocTitle] = useState('Sample Document');
  const [docSubtitle, setDocSubtitle] = useState('Generated by Primus Document Renderer');
  const [docContentType, setDocContentType] = useState<'PlainText' | 'Markdown' | 'Html'>('Markdown');
  const [docContent, setDocContent] = useState(`# Introduction

This is a **sample document** demonstrating the Primus Document Renderer module.

## Features

- Convert **Markdown** to PDF
- Support for plain text and HTML
- Tokenized download links
- Built-in self-test diagnostic

## Code Example

\`\`\`javascript
const result = await renderer.render({
  title: "My Document",
  contentType: "Markdown",
  content: "# Hello World"
});
\`\`\`

> This is a blockquote to demonstrate formatting.

Thank you for using Primus SaaS!`);
  const [docResult, setDocResult] = useState<any>(null);
  const [docError, setDocError] = useState<any>(null);
  const [isRenderingDoc, setIsRenderingDoc] = useState(false);
  const [selfTestResult, setSelfTestResult] = useState<any>(null);
  const [selfTestError, setSelfTestError] = useState<any>(null);
  const [isRunningSelfTest, setIsRunningSelfTest] = useState(false);
  const [selfTestMode, setSelfTestMode] = useState<'Basic' | 'Validation' | 'Complexity' | 'Full'>('Basic');

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
        response = await axios.post(`${apiBaseUrl}/auth/auth0`);
        setToken(response.data.access_token);
      } else if (selectedProvider === 'Azure') {
        response = await axios.post(`${apiBaseUrl}/auth/azure`);
        setToken(response.data.access_token);
      } else {
        if (!localEmail || !localPassword) {
          setIsLoading(false);
          setError(new Error('Email and password are required for Local JWT login.'));
          return;
        }
        response = await axios.post(`${apiBaseUrl}/auth/local`, {
          email: localEmail,
          password: localPassword
        });
        setToken(response.data.access_token);
      }
      setIsLoggedIn(true);
    } catch (err: any) {
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
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  const checkNotificationHealth = async () => {
    setIsHealthLoading(true);
    setHealthError(null);
    try {
      const response = await axios.get(`${apiBaseUrl}/notifications/health`);
      setHealth(response.data);
    } catch (err: any) {
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
      setNotificationResult(response.data);
    } catch (err: any) {
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
      setSmsResult(response.data);
    } catch (err: any) {
      setSmsError(err);
    } finally {
      setIsSendingSms(false);
    }
  };

  const loadTemplate = async () => {
    setIsLoadingTemplate(true);
    setTemplateLoadError(null);
    setTemplateSaveSuccess('');
    try {
      const response = await axios.get(`${apiBaseUrl}/notifications/templates/${previewType}/${previewChannel}`);
      setEditorContent(response.data.content || '');
    } catch (err: any) {
      setTemplateLoadError(err);
    } finally {
      setIsLoadingTemplate(false);
    }
  };

  const saveTemplate = async () => {
    setIsSavingTemplate(true);
    setTemplateSaveError(null);
    setTemplateSaveSuccess('');
    try {
      await axios.put(`${apiBaseUrl}/notifications/templates/${previewType}/${previewChannel}`, {
        content: editorContent
      });
      setTemplateSaveSuccess('Saved');
    } catch (err: any) {
      setTemplateSaveError(err);
    } finally {
      setIsSavingTemplate(false);
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
        email: previewEmail,
        content: editorContent
      });
      setPreviewResult(response.data.content || '');
    } catch (err: any) {
      setPreviewError(err);
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const fetchLogs = async (isAutoRefresh = false) => {
    // Only show loading state for manual fetches, not auto-refresh
    if (!isAutoRefresh) {
      setIsLoadingLogs(true);
    }
    setLogsError(null);
    try {
      const response = await axios.get(`${apiBaseUrl}/logs/recent`);
      setLogs(response.data.tail || '');
      setLogsMeta({ file: response.data.file, size: response.data.size });
    } catch (err: any) {
      setLogsError(err);
      if (!isAutoRefresh) {
        setLogs('');
        setLogsMeta(null);
      }
    } finally {
      if (!isAutoRefresh) {
        setIsLoadingLogs(false);
      }
    }
  };

  const fetchTelemetry = async (isAutoRefresh = false) => {
    if (!isAutoRefresh) {
      setIsLoadingTelemetry(true);
    }
    setTelemetryError(null);
    try {
      const response = await axios.get(`${apiBaseUrl}/telemetry/summary`);
      setTelemetry(response.data);
    } catch (err: any) {
      setTelemetryError(err);
      if (!isAutoRefresh) {
        setTelemetry(null);
      }
    } finally {
      if (!isAutoRefresh) {
        setIsLoadingTelemetry(false);
      }
    }
  };

  // Golden Path functions
  const fetchWhoami = async () => {
    if (!token) {
      setWhoamiError({ message: 'Please login first to use /whoami endpoint' });
      return;
    }
    setIsLoadingWhoami(true);
    setWhoamiError(null);
    try {
      const response = await axios.get(`${apiBaseUrl}/whoami`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setWhoamiResult(response.data);
    } catch (err: any) {
      setWhoamiError(err);
    } finally {
      setIsLoadingWhoami(false);
    }
  };

  const testLogging = async () => {
    setIsLoadingLogTest(true);
    setLogTestError(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/log/test`, {
        message: logTestMessage,
        level: logTestLevel,
        userId: 'golden-path-user',
        email: 'test@example.com'
      });
      setLogTestResult(response.data);
    } catch (err: any) {
      setLogTestError(err);
    } finally {
      setIsLoadingLogTest(false);
    }
  };

  const testNotification = async () => {
    setIsLoadingNotifTest(true);
    setNotifTestError(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/notifications/test`, {
        email: notifTestEmail,
        name: notifTestName,
        code: notifTestCode,
        link: 'http://localhost:5173/reset?token=demo123'
      });
      setNotifTestResult(response.data);
    } catch (err: any) {
      setNotifTestError(err);
    } finally {
      setIsLoadingNotifTest(false);
    }
  };

  // Document Renderer functions
  const renderDocument = async () => {
    setIsRenderingDoc(true);
    setDocError(null);
    setDocResult(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/documents/render/link`, {
        title: docTitle,
        subtitle: docSubtitle,
        contentType: docContentType,
        content: docContent,
        tenantId: 'demo-tenant'
      });
      setDocResult(response.data);
    } catch (err: any) {
      setDocError(err);
    } finally {
      setIsRenderingDoc(false);
    }
  };

  const downloadDocument = () => {
    if (docResult?.downloadToken) {
      window.open(`${apiBaseUrl}/documents/download/${docResult.downloadToken}`, '_blank');
    }
  };

  const runSelfTest = async () => {
    setIsRunningSelfTest(true);
    setSelfTestError(null);
    setSelfTestResult(null);
    try {
      const response = await axios.post(`${apiBaseUrl}/documents/self-test`, {
        mode: selfTestMode
      });
      setSelfTestResult(response.data);
    } catch (err: any) {
      setSelfTestError(err);
    } finally {
      setIsRunningSelfTest(false);
    }
  };

  // Fetch logs when Logs tab is opened
  useEffect(() => {
    if (activeTab === 'logs') {
      fetchLogs(false);
    }
  }, [activeTab]);

  useEffect(() => {
    if (!autoLogs) return;
    fetchLogs(false); // Initial fetch shows loading
    const id = setInterval(() => fetchLogs(true), 3000); // Auto-refresh is seamless
    return () => clearInterval(id);
  }, [autoLogs]);

  useEffect(() => {
    fetchTelemetry(false);
    if (!autoTelemetry) return;
    const id = setInterval(() => fetchTelemetry(true), 5000);
    return () => clearInterval(id);
  }, [autoTelemetry]);

  const renderSecureData = () => (
    <div className="card span-2" id="access">
      <div className="section-header">
        <div className="section-title">
          <Lock size={20} />
          <div>
            <h2>Secure Data Access</h2>
            <p className="subtitle">Pull protected sample data (secured API) to verify access.</p>
          </div>
        </div>
      </div>

      {!apiData && !isLoading && !error && (
        <div className="empty-state">
          <p>You have a valid <strong>{provider}</strong> token.</p>
          <button className="btn primary" onClick={fetchData}>
            <Server size={18} /> Fetch Secure Data
          </button>
        </div>
      )}

      {isLoading && (
        <div className="inline-loader">
          <div className="loader"></div>
          <span>Verifying identity token...</span>
        </div>
      )}

      {!isLoading && error && (
        <motion.div initial={{ x: -10 }} animate={{ x: 0 }} className="status-card error">
          <div className="status-heading danger">
            <AlertTriangle size={20} />
            <span>Access Denied ({error.response?.status || 'Error'})</span>
          </div>
          <p>The backend rejected the request. Your session might be expired or missing permissions.</p>
          <div className="code-block">{JSON.stringify(error.response?.data || error.message, null, 2)}</div>
        </motion.div>
      )}

      {!isLoading && apiData && (
        <motion.div initial={{ scale: 0.95 }} animate={{ scale: 1 }} className="status-card success">
          <div className="status-heading success">
            <CheckCircle size={20} />
            <span>Access Granted</span>
          </div>
          <p>Identity verified by <strong>PrimusSaaS</strong>. Data retrieved successfully.</p>
          {Array.isArray(apiData) && apiData.length > 0 ? (
            <div className="data-table">
              <div className="data-row head">
                {Object.keys(apiData[0]).map((key) => (
                  <span key={key}>{key}</span>
                ))}
              </div>
              {apiData.map((row: any, idx: number) => (
                <div className="data-row" key={idx}>
                  {Object.keys(apiData[0]).map((key) => (
                    <span key={key}>{String(row[key])}</span>
                  ))}
                </div>
              ))}
            </div>
          ) : (
            <div className="code-block">{JSON.stringify(apiData, null, 2)}</div>
          )}
        </motion.div>
      )}
    </div>
  );

  const renderHealth = () => (
    <div className="card">
      <div className="section-title">
        <Activity size={20} />
        <div>
          <h2>System Health</h2>
          <p className="subtitle">Snapshot of connected modules.</p>
        </div>
      </div>
      <div className="health-grid">
        <div className="health-item">
          <span className="label">Frontend</span>
          <strong className="success">Online</strong>
        </div>
        <div className="health-item">
          <span className="label">Backend</span>
          <strong className={telemetry ? 'success' : 'muted'}>{telemetry ? 'Online' : 'Unknown'}</strong>
        </div>
        <div className="health-item">
          <span className="label">Identity</span>
          <strong className={
            telemetry?.identity?.enabled === undefined
              ? 'muted'
              : telemetry.identity.enabled
                ? 'success'
                : 'danger'
          }>
            {telemetry?.identity?.enabled === undefined
              ? 'Unknown'
              : telemetry.identity.enabled
                ? 'Enabled'
                : 'Disabled'}
          </strong>
          {telemetry?.identity?.enabled === false && telemetry?.identity?.message && (
            <div className="helper">{telemetry.identity.message}</div>
          )}
        </div>
        <div className="health-item">
          <span className="label">Notifications</span>
          <strong className={health ? 'success' : healthError ? 'danger' : 'muted'}>
            {health ? 'Ready' : healthError ? 'Error' : 'Unknown'}
          </strong>
        </div>
      </div>
    </div>
  );

  // Dynamic chart data based on telemetry
  const getInsightsChartData = () => {
    if (!telemetry) return [];
    return [
      { label: 'Memory', value: Math.min(100, (telemetry.memory?.workingSetMB || 0) / 5), color: '#f26b4f' },
      { label: 'Threads', value: Math.min(100, (telemetry.runtime?.threadCount || 0) * 2), color: '#2fa66c' },
      { label: 'Uptime', value: Math.min(100, Math.log10((telemetry.server?.uptime?.seconds || 1) + 1) * 25), color: '#6366f1' },
      { label: 'GC Mem', value: Math.min(100, (telemetry.memory?.gcTotalMemoryMB || 0) / 2), color: '#f59e0b' },
    ];
  };

  const renderInsightsChart = () => {
    const data = getInsightsChartData();
    if (data.length === 0) return null;
    
    return (
      <div className="insights-chart-container">
        <h3 className="chart-title">Resource Usage</h3>
        <div className="insights-chart">
          {data.map((item) => (
            <div className="insights-bar-group" key={item.label}>
              <div className="insights-bar-track">
                <div 
                  className="insights-bar-fill" 
                  style={{ width: `${item.value}%`, backgroundColor: item.color }}
                />
              </div>
              <div className="insights-bar-label">
                <span>{item.label}</span>
                <span className="insights-bar-value">{Math.round(item.value)}%</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  const chartData = [
    { label: 'Income', value: 78 },
    { label: 'Alerts', value: 54 },
    { label: 'Tasks', value: 40 },
    { label: 'Uptime', value: 92 },
    { label: 'Engagement', value: 68 }
  ];

  const renderChartCard = () => (
    <div className="card chart-card">
      <div className="section-title">
        <div className="chart-icon">
          <Activity size={18} />
        </div>
        <div>
          <h2>Live Trends</h2>
          <p className="subtitle">Overview of what matters most—powered by simulated insights.</p>
        </div>
      </div>
      <div className="chart-body">
        {chartData.map((item) => (
          <div className="chart-column" key={item.label}>
            <div className="chart-bar" style={{ height: `${item.value}%` }} aria-label={`${item.label} ${item.value}%`} />
            <span className="chart-label">{item.label}</span>
          </div>
        ))}
      </div>
    </div>
  );

  const tabs = (
    <div className="tab-bar">
      <button className={activeTab === 'goldenpath' ? 'tab active' : 'tab'} onClick={() => setActiveTab('goldenpath')}>🏆 Golden Path</button>
      <button className={activeTab === 'overview' ? 'tab active' : 'tab'} onClick={() => setActiveTab('overview')}>Overview</button>
      <button className={activeTab === 'notifications' ? 'tab active' : 'tab'} onClick={() => setActiveTab('notifications')}>Notifications</button>
      <button className={activeTab === 'templates' ? 'tab active' : 'tab'} onClick={() => setActiveTab('templates')}>Templates</button>
      <button className={activeTab === 'insights' ? 'tab active' : 'tab'} onClick={() => setActiveTab('insights')}>App Insights</button>
      <button className={activeTab === 'logs' ? 'tab active' : 'tab'} onClick={() => setActiveTab('logs')}>Logs</button>
    </div>
  );

  /* ========================================================================
   * LOGIN / LANDING PAGE
   * Enterprise-grade split-screen layout with value proposition hero
   * and clean authentication options
   * ======================================================================== */
  if (!isLoggedIn) {
    return (
      <main className="login-page">
        {/* ----------------------------------------------------------------
         * LEFT PANEL: Brand Hero with Value Proposition
         * Communicates what the platform does at a glance
         * ---------------------------------------------------------------- */}
        <section className="login-hero">
          <motion.div 
            initial={{ opacity: 0, x: -30 }} 
            animate={{ opacity: 1, x: 0 }} 
            transition={{ duration: 0.6 }}
            className="hero-content"
          >
            {/* Brand Identity */}
            <div className="hero-brand">
              <div className="hero-logo">
                <Shield size={32} />
              </div>
              <span className="hero-brand-name">Primus</span>
            </div>

            {/* Main Value Proposition */}
            <h1 className="hero-headline">
              Secure, intelligent infrastructure for modern development teams.
            </h1>
            <p className="hero-subheadline">
              Enterprise-grade identity validation, structured logging, and smart notifications — all in reusable SDK modules.
            </p>

            {/* Key Features */}
            <ul className="hero-features">
              <li>
                <div className="feature-icon">
                  <Shield size={18} />
                </div>
                <div className="feature-text">
                  <strong>Multi-Issuer Identity</strong>
                  <span>Validate JWT tokens from Azure AD, Auth0, or custom issuers with one SDK.</span>
                </div>
              </li>
              <li>
                <div className="feature-icon">
                  <Activity size={18} />
                </div>
                <div className="feature-text">
                  <strong>Structured Logging</strong>
                  <span>PII masking, correlation IDs, and Application Insights out of the box.</span>
                </div>
              </li>
              <li>
                <div className="feature-icon">
                  <Mail size={18} />
                </div>
                <div className="feature-text">
                  <strong>Smart Notifications</strong>
                  <span>Email &amp; SMS with Liquid templates, SMTP/Twilio ready.</span>
                </div>
              </li>
            </ul>

            {/* Trust Indicators */}
            <div className="hero-trust">
              <div className="trust-item">
                <CheckCircle size={16} />
                <span>SOC 2 Ready</span>
              </div>
              <div className="trust-item">
                <Lock size={16} />
                <span>Zero PII Storage</span>
              </div>
              <div className="trust-item">
                <Server size={16} />
                <span>Client-Side SDK</span>
              </div>
            </div>
          </motion.div>

          {/* Background decoration */}
          <div className="hero-decoration"></div>
        </section>

        {/* ----------------------------------------------------------------
         * RIGHT PANEL: Authentication Form
         * Clean, accessible login options with clear hierarchy
         * ---------------------------------------------------------------- */}
        <section className="login-form-section">
          <motion.div 
            initial={{ opacity: 0, y: 20 }} 
            animate={{ opacity: 1, y: 0 }} 
            transition={{ duration: 0.5, delay: 0.2 }}
            className="login-form-container"
          >
            {/* Form Header */}
            <header className="form-header">
              <h2 className="form-title">Welcome back</h2>
              <p className="form-subtitle">Sign in to access the demo dashboard</p>
            </header>

            {/* SSO Options */}
            <div className="sso-section">
              <button 
                className="sso-btn auth0" 
                onClick={() => handleLogin('Auth0')} 
                disabled={isLoading}
                aria-label="Sign in with Auth0"
              >
                {isLoading && provider === 'Auth0' ? (
                  <div className="loader"></div>
                ) : (
                  <>
                    <div className="sso-icon auth0-icon">
                      <Shield size={20} />
                    </div>
                    <span>Continue with Auth0</span>
                  </>
                )}
              </button>

              <button 
                className="sso-btn azure" 
                onClick={() => handleLogin('Azure')} 
                disabled={isLoading}
                aria-label="Sign in with Azure AD"
              >
                {isLoading && provider === 'Azure' ? (
                  <div className="loader"></div>
                ) : (
                  <>
                    <div className="sso-icon azure-icon">
                      <Cloud size={20} />
                    </div>
                    <span>Continue with Azure AD</span>
                  </>
                )}
              </button>
            </div>

            {/* Divider */}
            <div className="auth-divider">
              <span>or sign in with credentials</span>
            </div>

            {/* Local JWT Form */}
            <form className="credentials-form" onSubmit={(e) => { e.preventDefault(); handleLogin('LocalJwt'); }}>
              <div className="form-field">
                <label htmlFor="email" className="field-label">Email address</label>
                <input 
                  id="email"
                  type="email" 
                  className="field-input" 
                  placeholder="you@company.com" 
                  value={localEmail} 
                  onChange={(e) => setLocalEmail(e.target.value)}
                  autoComplete="email"
                />
              </div>

              <div className="form-field">
                <div className="field-label-row">
                  <label htmlFor="password" className="field-label">Password</label>
                  <a href="#" className="field-link" onClick={(e) => e.preventDefault()}>Forgot password?</a>
                </div>
                <input 
                  id="password"
                  type="password" 
                  className="field-input" 
                  placeholder="••••••••••" 
                  value={localPassword} 
                  onChange={(e) => setLocalPassword(e.target.value)}
                  autoComplete="current-password"
                />
              </div>

              <button 
                type="submit" 
                className="submit-btn" 
                disabled={isLoading}
              >
                {isLoading && provider === 'LocalJwt' ? (
                  <div className="loader"></div>
                ) : (
                  <>
                    <LogIn size={18} />
                    <span>Sign in</span>
                  </>
                )}
              </button>
            </form>

            {/* Error Display */}
            {error && (
              <div className="auth-error" role="alert">
                <AlertTriangle size={16} />
                <span>{error.response?.data?.error || error.message}</span>
              </div>
            )}

            {/* Demo Credentials Hint */}
            <div className="demo-hint">
              <div className="hint-badge">Demo Mode</div>
              <p>Use <code>demo@primus.local</code> / <code>PrimusDemo123!</code></p>
            </div>

            {/* Footer */}
            <footer className="form-footer">
              <p>Powered by <strong>PrimusSaaS.Identity.Validator</strong></p>
              <div className="footer-links">
                <a href="#">Documentation</a>
                <span className="dot">·</span>
                <a href="#">Support</a>
              </div>
            </footer>
          </motion.div>
        </section>
      </main>
    );
  }

  return (
    <div className="page">
      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="shell">
        <header className="top-header">
          <div className="brand-block">
            <div className="brand-icon">
              <LayoutDashboard size={24} />
            </div>
            <div>
              <h1 className="page-title">Primus SDK Dashboard</h1>
            </div>
          </div>
          <div className="header-actions">
            <span className="chip success">
              <CheckCircle2 size={14} />
              {provider}
            </span>
            <button className="btn ghost sm" onClick={() => { setIsLoggedIn(false); setApiData(null); setError(null); setToken(''); setProvider(''); }}>
              <LogOut size={16} />
              Sign Out
            </button>
          </div>
        </header>

        {tabs}

        {/* Elite Welcome Hero */}
        <section className="welcome-hero">
          <div className="hero-content">
            <div className="hero-badge">
              <Zap size={14} />
              Live Integration Cockpit
            </div>
            <h2 className="hero-title">
              Welcome back to <span>Primus</span>
            </h2>
            <p className="hero-description">
              Your SDK modules are connected and operational. Test identity flows, monitor logs, send notifications, and track system health in real time.
            </p>
            <div className="hero-actions">
              <button className="btn primary" onClick={fetchData} disabled={isLoading}>
                {isLoading ? <div className="loader"></div> : <><Lock size={18} /> Fetch Secure Data</>}
              </button>
              <button className="btn ghost" onClick={() => fetchTelemetry(false)} disabled={isLoadingTelemetry}>
                {isLoadingTelemetry ? <div className="loader"></div> : <><RefreshCw size={18} /> Refresh Telemetry</>}
              </button>
            </div>
          </div>
          <div className="hero-metrics">
            <div className="metric-card">
              <div className="metric-icon green">
                <CheckCircle2 size={22} />
              </div>
              <div className="metric-info">
                <div className="metric-label">Authentication</div>
                <div className="metric-value success">{provider || 'Connected'}</div>
              </div>
            </div>
            <div className="metric-card">
              <div className="metric-icon blue">
                <Activity size={22} />
              </div>
              <div className="metric-info">
                <div className="metric-label">Server Status</div>
                <div className="metric-value">{telemetry?.server?.name || 'Ready'}</div>
              </div>
            </div>
            <div className="metric-card">
              <div className="metric-icon amber">
                <FileText size={22} />
              </div>
              <div className="metric-info">
                <div className="metric-label">Log Stream</div>
                <div className="metric-value">{autoLogs ? 'Live Auto' : logsMeta?.file ? 'Manual' : 'Idle'}</div>
              </div>
            </div>
          </div>
        </section>

        <div className="tab-content">
          {activeTab === 'goldenpath' && (
            <div className="modules-grid">
              {/* Identity: /whoami */}
              <div className="card featured">
                <div className="card-header">
                  <div className="card-icon identity">
                    <Shield size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Identity Validator</h3>
                    <p className="card-subtitle">Protected /whoami endpoint returning JWT claims. Demonstrates multi-issuer validation.</p>
                  </div>
                </div>
                <div className="form-row">
                  <button className="btn primary" onClick={fetchWhoami} disabled={isLoadingWhoami || !token}>
                    {isLoadingWhoami ? <div className="loader"></div> : 'Call /whoami'}
                  </button>
                  {!token && <span className="chip danger">Login required</span>}
                </div>
                {whoamiError && (
                  <div className="result-block danger">
                    <strong>Error:</strong> {whoamiError.response?.data?.error || whoamiError.message}
                    {whoamiError.response?.data?.hint && (
                      <div className="helper" style={{ marginTop: '6px' }}>
                        {whoamiError.response.data.hint}
                      </div>
                    )}
                  </div>
                )}
                {whoamiResult && (
                  <div className="result-block success">
                    <div style={{ marginBottom: '10px' }}>
                      <strong>✅ Authenticated!</strong>
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', fontSize: '14px' }}>
                      <div><strong>Subject:</strong> {whoamiResult.identity?.subject}</div>
                      <div><strong>Email:</strong> {whoamiResult.identity?.email || 'N/A'}</div>
                      <div><strong>Name:</strong> {whoamiResult.identity?.name || 'N/A'}</div>
                      <div><strong>Issuer:</strong> {whoamiResult.identity?.issuer}</div>
                    </div>
                    <details style={{ marginTop: '10px' }}>
                      <summary style={{ cursor: 'pointer', color: '#667eea' }}>View all claims ({whoamiResult.claims?.length})</summary>
                      <pre style={{ fontSize: '12px', marginTop: '10px', background: '#f3f4f6', padding: '10px', borderRadius: '4px', overflow: 'auto' }}>
                        {JSON.stringify(whoamiResult.claims, null, 2)}
                      </pre>
                    </details>
                  </div>
                )}
              </div>

              {/* Logging: /log/test */}
              <div className="card">
                <div className="card-header">
                  <div className="card-icon logging">
                    <Activity size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Logging Module</h3>
                    <p className="card-subtitle">Structured logging with custom fields and automatic PII redaction.</p>
                  </div>
                </div>
                <div className="form-column">
                  <div className="form-row">
                    <input 
                      className="input" 
                      placeholder="Log message" 
                      value={logTestMessage} 
                      onChange={(e) => setLogTestMessage(e.target.value)} 
                    />
                    <select 
                      className="input" 
                      value={logTestLevel} 
                      onChange={(e) => setLogTestLevel(e.target.value)}
                    >
                      <option value="Information">Information</option>
                      <option value="Warning">Warning</option>
                      <option value="Error">Error</option>
                    </select>
                  </div>
                  <button className="btn primary" onClick={testLogging} disabled={isLoadingLogTest}>
                    {isLoadingLogTest ? <div className="loader"></div> : 'Send Test Log'}
                  </button>
                </div>
                {logTestError && (
                  <div className="result-block danger">
                    <strong>Error:</strong> {logTestError.response?.data?.error || logTestError.message}
                  </div>
                )}
                {logTestResult && (
                  <div className="result-block success">
                    <div>✅ <strong>Logged!</strong></div>
                    <div style={{ fontSize: '14px', marginTop: '8px' }}>
                      Message: {logTestResult.message}<br/>
                      Level: {logTestResult.level}<br/>
                      Correlation ID: {logTestResult.correlationId}
                    </div>
                  </div>
                )}
              </div>

              {/* Notifications: /notifications/test */}
              <div className="card">
                <div className="card-header">
                  <div className="card-icon notifications">
                    <Mail size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Notifications</h3>
                    <p className="card-subtitle">Render Liquid templates for PasswordReset emails (preview only).</p>
                  </div>
                </div>
                <div className="form-column">
                  <div className="form-row">
                    <input 
                      className="input" 
                      placeholder="Email" 
                      value={notifTestEmail} 
                      onChange={(e) => setNotifTestEmail(e.target.value)} 
                    />
                    <input 
                      className="input" 
                      placeholder="Name" 
                      value={notifTestName} 
                      onChange={(e) => setNotifTestName(e.target.value)} 
                    />
                    <input 
                      className="input" 
                      placeholder="Code" 
                      value={notifTestCode} 
                      onChange={(e) => setNotifTestCode(e.target.value)} 
                    />
                  </div>
                  <button className="btn primary" onClick={testNotification} disabled={isLoadingNotifTest}>
                    {isLoadingNotifTest ? <div className="loader"></div> : 'Render Template'}
                  </button>
                </div>
                {notifTestError && (
                  <div className="result-block danger">
                    <strong>Error:</strong> {notifTestError.response?.data?.error || notifTestError.message}
                    {notifTestError.response?.data?.hint && (
                      <div style={{ fontSize: '12px', marginTop: '4px' }}>{notifTestError.response.data.hint}</div>
                    )}
                  </div>
                )}
                {notifTestResult && (
                  <div className="result-block success">
                    <div style={{ marginBottom: '10px' }}>✅ <strong>Template rendered!</strong> Type: {notifTestResult.templateType}</div>
                    <div style={{ marginBottom: '8px' }}>
                      <strong>Subject:</strong> {notifTestResult.rendered?.subject}
                    </div>
                    <details>
                      <summary style={{ cursor: 'pointer', color: '#667eea' }}>View rendered email body</summary>
                      <div 
                        style={{ 
                          marginTop: '10px', 
                          background: 'white', 
                          border: '1px solid #e5e7eb', 
                          borderRadius: '8px', 
                          padding: '16px',
                          maxHeight: '400px',
                          overflow: 'auto'
                        }}
                        dangerouslySetInnerHTML={{ __html: notifTestResult.rendered?.body || '' }}
                      />
                    </details>
                  </div>
                )}
              </div>

              {/* Document Renderer Module Card */}
              <div className="card" id="document-renderer">
                <div className="card-header">
                  <div className="card-icon documents">
                    <FileText size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Document Renderer</h3>
                    <p className="card-subtitle">Generate professional PDF documents from Markdown, HTML, or plain text.</p>
                  </div>
                </div>
                
                {/* Document Content Input */}
                <div style={{ marginBottom: '16px' }}>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 500, fontSize: '14px' }}>Title</label>
                  <input
                    className="input"
                    value={docTitle}
                    onChange={(e) => setDocTitle(e.target.value)}
                    placeholder="Document title..."
                    style={{ width: '100%' }}
                  />
                </div>
                
                <div style={{ marginBottom: '16px' }}>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 500, fontSize: '14px' }}>Subtitle</label>
                  <input
                    className="input"
                    value={docSubtitle}
                    onChange={(e) => setDocSubtitle(e.target.value)}
                    placeholder="Document subtitle..."
                    style={{ width: '100%' }}
                  />
                </div>
                
                <div style={{ marginBottom: '16px' }}>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 500, fontSize: '14px' }}>Content Type</label>
                  <select
                    className="input"
                    value={docContentType}
                    onChange={(e) => setDocContentType(e.target.value as 'PlainText' | 'Markdown' | 'Html')}
                    style={{ width: 'auto', minWidth: '150px' }}
                  >
                    <option value="PlainText">Plain Text</option>
                    <option value="Markdown">Markdown</option>
                    <option value="Html">HTML</option>
                  </select>
                </div>
                
                <div style={{ marginBottom: '16px' }}>
                  <label style={{ display: 'block', marginBottom: '4px', fontWeight: 500, fontSize: '14px' }}>Content</label>
                  <textarea
                    className="input textarea"
                    value={docContent}
                    onChange={(e) => setDocContent(e.target.value)}
                    rows={10}
                    placeholder="Enter your document content..."
                  />
                </div>
                
                <div className="form-row">
                  <button className="btn primary" onClick={renderDocument} disabled={isRenderingDoc}>
                    {isRenderingDoc ? <div className="loader"></div> : <>
                      <FileText size={16} />
                      Render PDF
                    </>}
                  </button>
                  {docResult?.downloadToken && (
                    <button className="btn success" onClick={downloadDocument}>
                      <Download size={16} />
                      Download PDF
                    </button>
                  )}
                </div>
                
                {docError && (
                  <div className="result-block danger" style={{ marginTop: '16px' }}>
                    <strong>Error:</strong> {docError.response?.data?.error || docError.message}
                  </div>
                )}
                
                {docResult && (
                  <div className="result-block success" style={{ marginTop: '16px' }}>
                    <div style={{ marginBottom: '10px' }}>✅ <strong>PDF Generated Successfully!</strong></div>
                    <div style={{ fontSize: '13px', display: 'grid', gap: '6px' }}>
                      <div><strong>Render Time:</strong> {docResult.durationMs}ms</div>
                      <div><strong>PDF Size:</strong> {(docResult.pdfSizeBytes / 1024).toFixed(1)} KB</div>
                      <div><strong>Download Token:</strong> <code style={{ background: '#f3f4f6', padding: '2px 6px', borderRadius: '4px' }}>{docResult.downloadToken}</code></div>
                      <div style={{ marginTop: '8px' }}>
                        <a href={`${apiBaseUrl}/documents/download/${docResult.downloadToken}`} target="_blank" rel="noopener noreferrer" style={{ color: '#667eea' }}>
                          Direct download link →
                        </a>
                      </div>
                    </div>
                  </div>
                )}
                
                {/* Self-Test Section */}
                <div style={{ marginTop: '24px', paddingTop: '24px', borderTop: '1px solid #e5e7eb' }}>
                  <div style={{ marginBottom: '16px' }}>
                    <strong style={{ fontSize: '16px' }}>Self-Test</strong>
                    <p style={{ fontSize: '13px', color: '#6b7280', margin: '4px 0 0 0' }}>
                      Run comprehensive diagnostics to verify PDF rendering capabilities.
                    </p>
                  </div>
                  
                  <div className="form-row" style={{ alignItems: 'flex-end' }}>
                    <div style={{ flex: '0 0 auto' }}>
                      <label style={{ display: 'block', marginBottom: '4px', fontWeight: 500, fontSize: '14px' }}>Test Mode</label>
                      <select
                        className="input"
                        value={selfTestMode}
                        onChange={(e) => setSelfTestMode(e.target.value as 'Basic' | 'Validation' | 'Complexity' | 'Full')}
                        style={{ width: 'auto', minWidth: '150px' }}
                      >
                        <option value="Basic">Basic (Quick check)</option>
                        <option value="Validation">Validation (Input tests)</option>
                        <option value="Complexity">Complexity (Stress tests)</option>
                        <option value="Full">Full (All tests)</option>
                      </select>
                    </div>
                    
                    <button className="btn primary" onClick={runSelfTest} disabled={isRunningSelfTest}>
                      {isRunningSelfTest ? <div className="loader"></div> : 'Run Self-Test'}
                    </button>
                  </div>
                  
                  {selfTestError && (
                    <div className="result-block danger" style={{ marginTop: '16px' }}>
                      <strong>Error:</strong> {selfTestError.response?.data?.error || selfTestError.message}
                    </div>
                  )}
                  
                  {selfTestResult && (
                    <div className={`result-block ${selfTestResult.overallStatus === 'Passed' ? 'success' : selfTestResult.overallStatus === 'Warning' ? 'warning' : 'danger'}`} style={{ marginTop: '16px' }}>
                      <div style={{ marginBottom: '10px' }}>
                        {selfTestResult.overallStatus === 'Passed' ? '✅' : selfTestResult.overallStatus === 'Warning' ? '⚠️' : '❌'}
                        <strong> Self-Test {selfTestResult.overallStatus}</strong>
                      </div>
                      <div style={{ fontSize: '13px', marginBottom: '12px' }}>
                        <strong>Summary:</strong> {selfTestResult.passed} passed, {selfTestResult.failed} failed, {selfTestResult.warnings} warnings
                        <span style={{ marginLeft: '16px' }}><strong>Duration:</strong> {selfTestResult.durationMs}ms</span>
                      </div>
                      
                      <details>
                        <summary style={{ cursor: 'pointer', color: '#667eea' }}>View test details ({selfTestResult.results?.length || 0} tests)</summary>
                        <div style={{ marginTop: '12px' }}>
                          {selfTestResult.results?.map((test: any, idx: number) => (
                            <div key={idx} style={{ 
                              padding: '8px 12px', 
                              marginBottom: '6px', 
                              background: test.status === 'Passed' ? '#ecfdf5' : test.status === 'Warning' ? '#fffbeb' : '#fef2f2',
                              borderRadius: '6px',
                              fontSize: '13px'
                            }}>
                              <div style={{ fontWeight: 500 }}>
                                {test.status === 'Passed' ? '✅' : test.status === 'Warning' ? '⚠️' : '❌'} {test.testName}
                              </div>
                              <div style={{ color: '#6b7280', marginTop: '2px' }}>{test.details}</div>
                              <div style={{ color: '#9ca3af', marginTop: '2px' }}>{test.durationMs}ms</div>
                            </div>
                          ))}
                        </div>
                      </details>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'overview' && (
            <div className="dashboard-grid">
              {renderSecureData()}
              {renderChartCard()}
              {renderHealth()}
            </div>
          )}

          {activeTab === 'notifications' && (
            <div className="dashboard-grid">
              <div className="card" id="notifications">
                <div className="card-header">
                  <div className="card-icon green">
                    <Activity size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Notification Health</h3>
                    <p className="card-subtitle">Ensure email and SMS paths are ready before sending messages.</p>
                  </div>
                </div>
                <div className="form-row">
                  <button className="btn primary" onClick={checkNotificationHealth} disabled={isHealthLoading}>
                    {isHealthLoading ? <div className="loader"></div> : 'Run Health Check'}
                  </button>
                  {health && (
                    <span className="chip success">
                      Email: {health.channels?.email} | SMS: {health.channels?.sms} | Logger: {health.channels?.logger}
                    </span>
                  )}
                  {healthError && (
                    <span className="chip danger">
                      {healthError.response?.status || ''} {healthError.message}
                    </span>
                  )}
                </div>
              </div>

              <div className="card">
                <div className="card-header">
                  <div className="card-icon notifications">
                    <Mail size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Send Welcome Email</h3>
                    <p className="card-subtitle">Quickly send a sample welcome note using your configured delivery paths.</p>
                  </div>
                </div>
                <div className="form-column">
                  <div className="form-row">
                    <input className="input" placeholder="Recipient email" value={notificationEmail} onChange={(e) => setNotificationEmail(e.target.value)} />
                    <input className="input" placeholder="Recipient name" value={notificationName} onChange={(e) => setNotificationName(e.target.value)} />
                    <button className="btn primary" onClick={sendWelcomeEmail} disabled={isSendingNotification}>
                      {isSendingNotification ? <div className="loader"></div> : <><Send size={18} /> Send Email</>}
                    </button>
                  </div>

                  {notificationResult && (
                    <div className="result-block success">
                      <strong>Sent via:</strong> {notificationResult.channel || 'Logger'} | queued: {String(notificationResult.queued)}
                    </div>
                  )}

                  {notificationError && (
                    <div className="result-block danger">
                      <strong>Error:</strong> {notificationError.response?.data?.detail || notificationError.message}
                    </div>
                  )}
                </div>
              </div>

              <div className="card">
                <div className="card-header">
                  <div className="card-icon amber">
                    <Phone size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Send SMS</h3>
                    <p className="card-subtitle">Text a demo message to confirm the SMS experience end-to-end.</p>
                  </div>
                </div>
                <div className="form-column">
                  <div className="form-row">
                    <input className="input" placeholder="Recipient phone (E.164)" value={smsPhone} onChange={(e) => setSmsPhone(e.target.value)} />
                    <input className="input" placeholder="Message" value={smsMessage} onChange={(e) => setSmsMessage(e.target.value)} />
                    <button className="btn primary" onClick={sendSms} disabled={isSendingSms}>
                      {isSendingSms ? <div className="loader"></div> : <><Send size={18} /> Send SMS</>}
                    </button>
                  </div>

                  {smsResult && (
                    <div className="result-block success">
                      <strong>Sent via:</strong> {smsResult.channel || 'Logger'} | queued: {String(smsResult.queued)}
                    </div>
                  )}

                  {smsError && (
                    <div className="result-block danger">
                      <strong>Error:</strong> {smsError.response?.data?.detail || smsError.message}
                      <div className="helper">Verify Twilio config and number formatting (E.164) before retrying.</div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'templates' && (
            <div className="dashboard-grid">
              <div className="card span-2" id="templates">
                <div className="card-header">
                  <div className="card-icon notifications">
                    <Mail size={24} />
                  </div>
                  <div className="card-title-group">
                    <h3 className="card-title">Template Preview</h3>
                    <p className="card-subtitle">Preview message templates (no send) to see exactly what customers receive.</p>
                  </div>
                </div>

                <div className="form-column">
                  <div className="form-row">
                    <select className="input" value={previewType} onChange={(e) => setPreviewType(e.target.value)}>
                      {typeOptions.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
                    </select>
                    <select className="input" value={previewChannel} onChange={(e) => setPreviewChannel(e.target.value)}>
                      {channelOptions.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
                    </select>
                    <input className="input" placeholder="Message" value={previewMessage} onChange={(e) => setPreviewMessage(e.target.value)} />
                    <input className="input" placeholder="Phone" value={previewPhone} onChange={(e) => setPreviewPhone(e.target.value)} />
                    <input className="input" placeholder="Name" value={previewName} onChange={(e) => setPreviewName(e.target.value)} />
                    <input className="input" placeholder="Email" value={previewEmail} onChange={(e) => setPreviewEmail(e.target.value)} />
                  </div>

                  <textarea className="input textarea" placeholder="Edit template content (Liquid) or load from file" value={editorContent} onChange={(e) => setEditorContent(e.target.value)} />

                  <div className="form-row wrap">
                    <button className="btn ghost" onClick={loadTemplate} disabled={isLoadingTemplate}>
                      {isLoadingTemplate ? <div className="loader"></div> : 'Load from file'}
                    </button>
                    <button className="btn success" onClick={saveTemplate} disabled={isSavingTemplate}>
                      {isSavingTemplate ? <div className="loader"></div> : 'Save to file'}
                    </button>
                    <button className="btn primary" onClick={previewTemplate} disabled={isPreviewLoading}>
                      {isPreviewLoading ? <div className="loader"></div> : 'Preview'}
                    </button>
                  </div>

                  {previewResult && <pre className="preview-block">{previewResult}</pre>}

                  {previewError && (
                    <div className="status-card error">
                      <strong>Error:</strong> {previewError.response?.data?.error || previewError.message}
                    </div>
                  )}

                  {templateLoadError && (
                    <div className="status-card error">
                      <strong>Load error:</strong> {templateLoadError.response?.data?.error || templateLoadError.message}
                    </div>
                  )}

                  {templateSaveError && (
                    <div className="status-card error">
                      <strong>Save error:</strong> {templateSaveError.response?.data?.error || templateSaveError.message}
                    </div>
                  )}

                  {templateSaveSuccess && <div className="status-card success">{templateSaveSuccess}</div>}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'insights' && (
            <div className="dashboard-grid">
              <div className="card span-2" id="insights">
                <div className="section-title">
                  <BarChart3 size={20} />
                  <div>
                    <h2>Application Insights Dashboard</h2>
                    <p className="subtitle">Live service health so business can see the platform is up, responsive, and instrumented.</p>
                  </div>
                </div>

                <div className="form-row">
                  <button className="btn primary" onClick={() => fetchTelemetry(false)} disabled={isLoadingTelemetry}>
                    {isLoadingTelemetry ? <div className="loader"></div> : 'Refresh'}
                  </button>
                  <label className="checkbox">
                    <input type="checkbox" checked={autoTelemetry} onChange={(e) => setAutoTelemetry(e.target.checked)} />
                    Auto-refresh (5s)
                  </label>
                  {telemetry?.applicationInsights?.enabled && (
                    <a href={telemetry.applicationInsights.portalUrl} target="_blank" rel="noopener noreferrer" className="link">
                      Open Azure Portal →
                    </a>
                  )}
                  {telemetryError && (
                    <span className="chip danger">
                      {telemetryError.response?.data?.message || telemetryError.message}
                    </span>
                  )}
                </div>

                {telemetry && (
                  <div className="insights-layout">
                    <div className="insights-summary">
                      <div className="summary-pill" data-tone={telemetry.applicationInsights?.enabled ? 'success' : 'danger'}>
                        <Wifi size={16} />
                        <span>{telemetry.applicationInsights?.enabled ? 'App Insights connected' : 'App Insights disabled'}</span>
                      </div>
                      <div className="summary-pill">
                        <Clock size={16} />
                        <span>Uptime: {telemetry.server?.uptime?.formatted || '—'}</span>
                      </div>
                      <div className="summary-pill">
                        <Server size={16} />
                        <span>Server: {telemetry.server?.name || '—'}</span>
                      </div>
                    </div>

                    <div className="metric-grid">
                      <div className="metric-tile" data-tone={telemetry.applicationInsights?.enabled ? 'success' : 'danger'}>
                        <div className="metric-heading">
                          <Wifi size={18} />
                          <strong>{telemetry.applicationInsights?.enabled ? 'AI Connected' : 'AI Disabled'}</strong>
                        </div>
                        {telemetry.applicationInsights?.instrumentationKey && (
                          <div className="metric-sub">Key: {telemetry.applicationInsights.instrumentationKey.substring(0, 8)}...</div>
                        )}
                        {!telemetry.applicationInsights?.enabled && (
                          <div className="metric-sub">Configure connection string to enable</div>
                        )}
                      </div>

                      <div className="metric-tile info">
                        <div className="metric-heading">
                          <Server size={18} />
                          <strong>{telemetry.server?.name || 'Server'}</strong>
                        </div>
                        <div className="metric-sub">PID: {telemetry.runtime?.processId}</div>
                      </div>

                      <div className="metric-tile lilac">
                        <div className="metric-heading">
                          <Clock size={18} />
                          <strong>Uptime</strong>
                        </div>
                        <div className="metric-sub prominent">{telemetry.server?.uptime?.formatted || '—'}</div>
                      </div>

                      <div className="metric-tile pink">
                        <div className="metric-heading">
                          <HardDrive size={18} />
                          <strong>Memory</strong>
                        </div>
                        <div className="metric-sub prominent">{telemetry.memory?.workingSetMB || 0} MB working set</div>
                        <div className="metric-sub">GC: {telemetry.memory?.gcTotalMemoryMB || 0} MB</div>
                      </div>

                      <div className="metric-tile amber">
                        <div className="metric-heading">
                          <Cpu size={18} />
                          <strong>Threads</strong>
                        </div>
                        <div className="metric-sub prominent">{telemetry.runtime?.threadCount || 0} active</div>
                      </div>

                      <div className="metric-tile teal">
                        <div className="metric-heading">
                          <Activity size={18} />
                          <strong>Runtime</strong>
                        </div>
                        <div className="metric-sub wrap">{telemetry.runtime?.framework || '—'}</div>
                      </div>
                    </div>

                    {renderInsightsChart()}
                  </div>
                )}

                {telemetry?.timestamp && (
                  <div className="footnote align-right">
                    Last updated: {new Date(telemetry.timestamp).toLocaleTimeString()}
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'logs' && (
            <div className="dashboard-grid">
              <div className="card span-2" id="logs">
                <div className="section-title">
                  <Activity size={20} />
                  <div>
                    <h2>API Logs</h2>
                    <p className="subtitle">Recent application events for transparency on what just happened.</p>
                  </div>
                </div>

                <div className="log-controls">
                  <button className="btn primary" onClick={() => fetchLogs(false)} disabled={isLoadingLogs}>
                    Fetch latest logs
                  </button>
                  <label className="checkbox">
                    <input
                      type="checkbox"
                      checked={autoLogs}
                      onChange={(e) => setAutoLogs(e.target.checked)}
                    />
                    Live refresh (3s)
                  </label>
                  {logsMeta?.file && (
                    <span className="chip success">
                      {logsMeta.file} ({logsMeta.size} bytes)
                    </span>
                  )}
                  <select
                    className="input log-filter-select"
                    value={logFilterLevel}
                    onChange={(e) => setLogFilterLevel(e.target.value as any)}
                  >
                    <option value="All">All levels</option>
                    <option value="Error">Errors</option>
                    <option value="Warn">Warnings</option>
                    <option value="Info">Info</option>
                  </select>
                  <input
                    className="input log-search"
                    placeholder="Search text"
                    value={logSearch}
                    onChange={(e) => setLogSearch(e.target.value)}
                  />
                  {logsError && (
                    <span className="chip danger">
                      {logsError.response?.data?.message || logsError.message}
                    </span>
                  )}
                </div>

                <div className="log-table-container">
                  <table className="log-table-modern">
                    <thead>
                      <tr>
                        <th style={{ width: '50px' }}>#</th>
                        <th style={{ width: '180px' }}>Timestamp</th>
                        <th style={{ width: '80px' }}>Level</th>
                        <th>Message</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(() => {
                        const lines = (logs || '').split('\n').filter(Boolean);
                        const parsed = lines.map((line, idx) => {
                          // Try to parse as JSON log format first
                          try {
                            const json = JSON.parse(line);
                            const timestamp = json.timestamp 
                              ? new Date(json.timestamp).toISOString().replace('T', ' ').replace('Z', '') 
                              : '';
                            const level = (json.level || json.Level || 'INFO').toUpperCase();
                            const message = json.message || json.Message || JSON.stringify(json);
                            
                            // Map log levels
                            let displayLevel = 'Info';
                            if (/error/i.test(level)) displayLevel = 'Error';
                            else if (/warn/i.test(level)) displayLevel = 'Warn';
                            else if (/debug|trace/i.test(level)) displayLevel = 'Debug';
                            
                            return { idx: idx + 1, timestamp, level: displayLevel, message, raw: line };
                          } catch {
                            // Fallback: Parse old log format [2025-11-30 04:41:11Z] Message...
                            const timestampMatch = line.match(/^\[([^\]]+)\]/);
                            const timestamp = timestampMatch ? timestampMatch[1] : '';
                            const rest = timestampMatch ? line.slice(timestampMatch[0].length).trim() : line;
                            
                            // Detect level from content
                            let level = 'Info';
                            if (/error|exception|fail/i.test(rest)) level = 'Error';
                            else if (/warn/i.test(rest)) level = 'Warn';
                            else if (/debug/i.test(rest)) level = 'Debug';
                            
                            return { idx: idx + 1, timestamp, level, message: rest, raw: line };
                          }
                        });

                        const filtered = parsed.filter((entry) => {
                          const levelMatch =
                            logFilterLevel === 'All' ||
                            (logFilterLevel === 'Error' && entry.level === 'Error') ||
                            (logFilterLevel === 'Warn' && entry.level === 'Warn') ||
                            (logFilterLevel === 'Info' && entry.level === 'Info');
                          const searchMatch = logSearch.trim()
                            ? entry.raw.toLowerCase().includes(logSearch.trim().toLowerCase())
                            : true;
                          return levelMatch && searchMatch;
                        });

                        if (filtered.length === 0) {
                          return (
                            <tr className="log-empty-row">
                              <td colSpan={4}>
                                {logs ? 'No log lines match your filters.' : 'Log file not found yet. Generate activity to create it.'}
                              </td>
                            </tr>
                          );
                        }

                        return filtered.map((entry) => (
                          <tr key={entry.idx} className={`log-level-${entry.level.toLowerCase()}`}>
                            <td className="log-idx">{entry.idx}</td>
                            <td className="log-timestamp">{entry.timestamp}</td>
                            <td>
                              <span className={`log-level-badge ${entry.level.toLowerCase()}`}>
                                {entry.level}
                              </span>
                            </td>
                            <td className="log-message">{entry.message}</td>
                          </tr>
                        ));
                      })()}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}
        </div>
      </motion.div>
    </div>
  );
}

export default App;
