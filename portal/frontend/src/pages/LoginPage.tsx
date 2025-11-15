import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../providers/AuthProvider';
import { getErrorMessage } from '../services/apiClient';
import './LoginPage.css';

export const LoginPage = () => {
  const navigate = useNavigate();
  const { login, isLoading } = useAuth();
  const [email, setEmail] = useState('admin@primussaas.com');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState<string | null>(null);

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

  return (
    <div className="login">
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
      </form>
    </div>
  );
};
