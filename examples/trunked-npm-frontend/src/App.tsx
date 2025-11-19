import { useEffect, useState } from 'react';
import './App.css';

type HealthPayload = {
  status: string;
  mode: string;
  authEnforced: boolean;
};

type DashboardPayload = {
  metrics: {
    activeUsers: number;
    deploymentsToday: number;
    serviceHealth: string;
    latencyMs: number;
  };
  applications: Array<{ id: string; name: string; status: string; lastDeployment: string }>;
  releases: Array<{ module: string; version: string; status: string }>;
  primusUser: { name?: string; email?: string } | null;
  authenticated: boolean;
};

type NotificationPayload = {
  notifications: Array<{
    id: string;
    message: string;
    severity: 'info' | 'warning';
    createdAt: string;
  }>;
};

const defaultBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:4000';

function App() {
  const [baseUrl, setBaseUrl] = useState(defaultBaseUrl);
  const [token, setToken] = useState('');
  const [health, setHealth] = useState<HealthPayload | null>(null);
  const [dashboard, setDashboard] = useState<DashboardPayload | null>(null);
  const [notifications, setNotifications] = useState<NotificationPayload['notifications']>([]);
  const [publicResponse, setPublicResponse] = useState<string>('Not requested yet');
  const [loadingProtected, setLoadingProtected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setHealth(null);
    fetchJson<HealthPayload>('/health', { baseUrl })
      .then(setHealth)
      .catch((err) => setError(err.message));
  }, [baseUrl]);

  const loadProtectedData = async () => {
    setLoadingProtected(true);
    setError(null);

    try {
      const [dash, notes] = await Promise.all([
        fetchJson<DashboardPayload>('/api/dashboard', { baseUrl, token, requiresAuth: true }),
        fetchJson<NotificationPayload>('/api/notifications', {
          baseUrl,
          token,
          requiresAuth: true
        })
      ]);
      setDashboard(dash);
      setNotifications(notes.notifications);
    } catch (err) {
      setError((err as Error).message);
      setDashboard(null);
      setNotifications([]);
    } finally {
      setLoadingProtected(false);
    }
  };

  const callPublicEndpoint = async () => {
    try {
      const response = await fetchJson<{ message: string; timestamp: string }>('/api/public', {
        baseUrl
      });
      setPublicResponse(`${response.message} @ ${response.timestamp}`);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const authEnabled = health?.authEnforced ?? false;

  return (
    <div className="app-shell">
      <header>
        <h1>New App → Authenticated App</h1>
        <p>
          This dashboard represents a “real” Primus module that originally launched without any auth.
          Flip <code>PRIMUS_ENFORCE_AUTH</code> to watch the same routes become protected the moment
          the published <code>primus-identity-validator</code> middleware is wired in.
        </p>
      </header>

      <section className="status-grid">
        <article className="status-card">
          <h2>Before Integration</h2>
          <p>
            Set <code>PRIMUS_ENFORCE_AUTH=false</code> to show the legacy behaviour—every dashboard
            call succeeds without tokens.
          </p>
        </article>
        <article className="status-card current">
          <h2>After Integration</h2>
          <p>
            Current backend mode: <strong>{authEnabled ? 'Auth Enforced' : 'Open'}</strong>{' '}
            (validation mode: {health?.mode ?? 'loading…'}).
          </p>
          <p>
            When enforced, supply a Primus Portal or Azure AD token to reach any protected route.
          </p>
        </article>
      </section>

      <section className="config-panel">
        <label>
          Backend URL
          <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} />
        </label>
        <div className="token-row">
          <label>
            Access Token (Bearer)
            <textarea
              rows={4}
              placeholder="Paste a Primus Portal or Azure AD token"
              value={token}
              onChange={(event) => setToken(event.target.value)}
            />
          </label>
          <div className="actions">
            <button onClick={callPublicEndpoint}>Test Public Endpoint</button>
            <button onClick={loadProtectedData} disabled={loadingProtected}>
              {loadingProtected ? 'Loading…' : 'Load Dashboard Data'}
            </button>
            <p className="hint">
              Tokens are obtained exactly as the npm README describes (Primus login or{' '}
              <code>az account get-access-token</code> for Azure AD).
            </p>
            <p className="public-response">{publicResponse}</p>
          </div>
        </div>
      </section>

      {error && <p className="error">Last request failed: {error}</p>}

      <section className="dashboard-grid">
        <article className="panel">
          <header>
            <h3>Key Metrics</h3>
            <span className="tag">{dashboard?.metrics ? 'Protected' : 'Locked'}</span>
          </header>
          {dashboard ? (
            <dl>
              <div>
                <dt>Active users</dt>
                <dd>{dashboard.metrics.activeUsers.toLocaleString()}</dd>
              </div>
              <div>
                <dt>Deployments today</dt>
                <dd>{dashboard.metrics.deploymentsToday}</dd>
              </div>
              <div>
                <dt>Service health</dt>
                <dd>{dashboard.metrics.serviceHealth}</dd>
              </div>
              <div>
                <dt>Avg latency</dt>
                <dd>{dashboard.metrics.latencyMs} ms</dd>
              </div>
            </dl>
          ) : (
            <p className="placeholder">
              Run <strong>Load Dashboard Data</strong> with auth enabled to populate these metrics.
            </p>
          )}
        </article>

        <article className="panel">
          <header>
            <h3>Applications</h3>
            <span className="tag">{dashboard?.applications ? 'Protected' : 'Locked'}</span>
          </header>
          {dashboard ? (
            <ul className="app-list">
              {dashboard.applications.map((app) => (
                <li key={app.id}>
                  <div>
                    <strong>{app.name}</strong>
                    <p>{app.status}</p>
                  </div>
                  <small>Last deploy: {new Date(app.lastDeployment).toLocaleString()}</small>
                </li>
              ))}
            </ul>
          ) : (
            <p className="placeholder">Requires the Primus middleware to respond.</p>
          )}
        </article>

        <article className="panel">
          <header>
            <h3>Release Timeline</h3>
            <span className="tag">{dashboard?.releases ? 'Protected' : 'Locked'}</span>
          </header>
          {dashboard ? (
            <ul className="release-list">
              {dashboard.releases.map((release) => (
                <li key={release.module}>
                  <strong>{release.module}</strong>
                  <div>{release.version}</div>
                  <small>{release.status}</small>
                </li>
              ))}
            </ul>
          ) : (
            <p className="placeholder">Deployments remain hidden until auth is enforced.</p>
          )}
        </article>

        <article className="panel notifications">
          <header>
            <h3>Notification Center</h3>
            <span className="tag">{notifications.length ? 'Protected' : 'Locked'}</span>
          </header>
          {notifications.length ? (
            <ul>
              {notifications.map((note) => (
                <li key={note.id}>
                  <div className={`severity ${note.severity}`}>{note.severity}</div>
                  <p>{note.message}</p>
                  <small>{new Date(note.createdAt).toLocaleString()}</small>
                </li>
              ))}
            </ul>
          ) : (
            <p className="placeholder">
              The notification feed is inaccessible without a validated Primus identity.
            </p>
          )}
        </article>
      </section>

      {dashboard?.primusUser && (
        <section className="user-pill">
          Signed in as <strong>{dashboard.primusUser.name ?? dashboard.primusUser.email}</strong>
        </section>
      )}
    </div>
  );
}

export default App;

type FetchOptions = {
  baseUrl: string;
  token?: string;
  requiresAuth?: boolean;
};

async function fetchJson<T>(path: string, options: FetchOptions): Promise<T> {
  if (options.requiresAuth && !options.token) {
    throw new Error('Provide a Primus token to call protected routes.');
  }

  const response = await fetch(`${options.baseUrl}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(options.token ? { Authorization: `Bearer ${options.token.trim()}` } : {})
    }
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(
      `Request to ${path} failed (${response.status}): ${body || response.statusText}`
    );
  }

  return response.json() as Promise<T>;
}
