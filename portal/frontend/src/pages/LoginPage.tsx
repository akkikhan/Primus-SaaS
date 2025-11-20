import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import { useAuth } from '../providers/AuthProvider';
import { getErrorMessage } from '../services/apiClient';
import { isAzureAdConfigured } from '../auth/msalConfig';
import './LoginPage.css';

export const LoginPage = () => {
  const navigate = useNavigate();
  const { instance } = useMsal();
  const { login, loginWithAzure, isLoading } = useAuth();
  const [email, setEmail] = useState('admin@primussaas.com');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState<string | null>(null);
  const [azureError, setAzureError] = useState<string | null>(null);
  const [isAzureLoading, setIsAzureLoading] = useState(false);
  const azureConfigured = isAzureAdConfigured;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);

    try {
      await login(email, password);
      navigate('/', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const handleAzureLogin = async () => {
    if (!azureConfigured) {
      setAzureError('Azure AD login has not been configured.');
      return;
    }

    setAzureError(null);
    setIsAzureLoading(true);

    try {
      const response = await instance.loginPopup({
        scopes: ['openid', 'profile', 'email'],
        prompt: 'select_account',
      });

      if (!response.idToken) {
        throw new Error('Azure AD did not return an ID token.');
      }

      await loginWithAzure(response.idToken);
      navigate('/', { replace: true });
    } catch (err) {
      const errorCode = (err as { errorCode?: string }).errorCode;
      const isUserCancelled = errorCode === 'user_cancelled' || errorCode === 'popup_window_error';

      if (!isUserCancelled) {
        const msalMessage = (err as { errorMessage?: string }).errorMessage;
        setAzureError(msalMessage ?? getErrorMessage(err));
      }
    } finally {
      setIsAzureLoading(false);
    }
  };

  return (
    <div className="login">
      <div className="login__wrapper">
        <form className="login__card" onSubmit={handleSubmit}>
          <h1>Primus SaaS Portal</h1>
          <p className="login__subtitle">Sign in with your admin credentials.</p>

          <label>
            Email
            <input
              type="email"
              value={email}
              onChange={event => setEmail(event.target.value)}
              required
            />
          </label>

          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={event => setPassword(event.target.value)}
              required
            />
          </label>

          {error && <p className="login__error">{error}</p>}

          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Signing in…' : 'Sign in'}
          </button>

          <p className="login__hint">
            Default admin account: admin@primussaas.com / Admin123!
          </p>
        </form>

        <div className="login__card login__card--secondary">
          <h2>Enterprise SSO</h2>
          <p className="login__subtitle">Connect with your Azure Active Directory account.</p>

          {azureError && <p className="login__error">{azureError}</p>}

          <button
            type="button"
            className="login__azure-button"
            onClick={handleAzureLogin}
            disabled={!azureConfigured || isAzureLoading}
          >
            {isAzureLoading ? 'Connecting…' : 'Sign in with Azure AD'}
          </button>

          {!azureConfigured && (
            <p className="login__hint">
              Provide <code>VITE_AZURE_AD_CLIENT_ID</code> and <code>VITE_AZURE_AD_TENANT_ID</code> to
              enable Azure AD login.
            </p>
          )}
        </div>
      </div>
    </div>
  );
};
