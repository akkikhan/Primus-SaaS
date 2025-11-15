import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import ToastContainer from './ToastContainer';
import './MainLayout.css';

export const MainLayout = () => (
  <div className="layout">
    <Sidebar />
    <div className="layout__content">
      <TopBar />
      <main>
        <Outlet />
      </main>
    </div>
    <ToastContainer />
  </div>
);
