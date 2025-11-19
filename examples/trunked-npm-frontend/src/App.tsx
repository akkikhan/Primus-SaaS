import { useState } from 'react';
import './App.css';

type Endpoint = {
  id: string;
  label: string;
  path: string;
  requiresAuth?: boolean;
};

const endpoints: Endpoint[] = [
  { id: 'health', label: 'Health', path: '/health' },
  { id: 'public', label: 'Public', path: '/api/public' },
  { id: 'profile', label: 'Profile', path: '/api/profile', requiresAuth: true },
  { id: 'admin', label: 'Admin', path: '/api/admin', requiresAuth: true },
  { id: 'management', label: 'Management', path: '/api/management', requiresAuth: true }
];

const defaultBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:4000';

function App() {
  const [baseUrl, setBaseUrl] = useState(defaultBaseUrl);
  const [token, setToken] = useState('');
  const [activeRequest, setActiveRequest] = useState<string | null>(null);
  const [results, setResults] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const callEndpoint = async (endpoint: Endpoint) => {
    setActiveRequest(endpoint.id);
    setError(null);

    try {
      const response = await fetch(`${baseUrl}${endpoint.path}`, {
        headers: {
          'Content-Type': 'application/json',
          ...(endpoint.requiresAuth && token
            ? {
                Authorization: `Bearer ${token.trim()}`
              }
            : {})
        }
      });

      const body = await response.text();
      let parsed = body;

      try {
        parsed = JSON.stringify(JSON.parse(body), null, 2);
      } catch {
        // Non JSON response; show raw text
      }

      setResults((prev) => ({
        ...prev,
        [endpoint.id]: `Status: ${response.status}\n${parsed}`
      }));
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setActiveRequest(null);
    }
  };

  return (
    <div className="app-shell">
      <header>
        <h1>Primus Identity Validator Demo</h1>
        <p>
          Frontend helper for the trunked backend sample. Configure the backend URL, paste a JWT
          token, and exercise the routes that are protected by the published
          <code> primus-identity-validator </code> middleware.
        </p>
      </header>

      <section className="config-panel">
        <label>
          Backend URL
          <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} />
        </label>
        <label>
          Access Token (Bearer)
          <textarea
            rows={5}
            placeholder="Paste a Primus Portal or Azure AD token"
            value={token}
            onChange={(event) => setToken(event.target.value)}
          />
        </label>
        <p className="hint">
          Need tokens? Follow the npm README instructions (Primus login or <code>az account
          get-access-token</code> for Azure AD).
        </p>
      </section>

  <section className="endpoint-panel">
        {endpoints.map((endpoint) => (
          <article key={endpoint.id} className="endpoint-card">
            <div className="endpoint-header">
              <div>
                <h2>{endpoint.label}</h2>
                <p className="path">
                  {endpoint.path}{' '}
                  {endpoint.requiresAuth && <span className="badge">Requires token</span>}
                </p>
              </div>
              <button
                disabled={!!activeRequest}
                onClick={() => callEndpoint(endpoint)}
              >
                {activeRequest === endpoint.id ? 'Calling…' : 'Send request'}
              </button>
            </div>
            <pre>{results[endpoint.id] ?? 'Awaiting request…'}</pre>
          </article>
        ))}
      </section>

      {error && <p className="error">Last request failed: {error}</p>}
    </div>
  );
}

export default App;
