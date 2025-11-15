import { useNavigate } from 'react-router-dom';
import { useAuth } from '../providers/AuthProvider';
import './TopBar.css';

export const TopBar = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <header className="topbar">
      <div className="topbar__title">Welcome back, {user?.email ?? 'Admin'}</div>
      <button className="topbar__logout" onClick={handleLogout}>
        Logout
      </button>
    </header>
  );
};
