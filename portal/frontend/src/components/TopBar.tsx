import { useAuth } from '../hooks/useAuth';
import './TopBar.css';

export const TopBar = () => {
  const { user, logout } = useAuth();

  return (
    <header className="topbar">
      <div className="topbar__title">Welcome back, {user?.email ?? 'Admin'}</div>
      <button className="topbar__logout" onClick={logout}>
        Logout
      </button>
    </header>
  );
};
