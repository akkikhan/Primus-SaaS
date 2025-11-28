import { NavLink } from 'react-router-dom';
import './Sidebar.css';

const links = [
  { to: '/', label: 'Dashboard' },
  { to: '/modules', label: 'Modules' },
  { to: '/applications', label: 'Applications' },
  { to: '/notifications', label: 'Notifications' },
  { to: '/upgrade-manager', label: 'Upgrade Manager' }
];

export const Sidebar = () => (
  <aside className="sidebar">
    <div className="sidebar__brand">Primus SaaS</div>
    <nav>
      {links.map(link => (
        <NavLink
          key={link.to}
          to={link.to}
          className={({ isActive }) =>
            isActive ? 'sidebar__link sidebar__link--active' : 'sidebar__link'
          }
          end={link.to === '/'}
        >
          {link.label}
        </NavLink>
      ))}
    </nav>
  </aside>
);
