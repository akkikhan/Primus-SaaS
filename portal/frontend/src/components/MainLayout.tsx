import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import ToastContainer from './ToastContainer';
import VoiceOverlay from './VoiceOverlay';
import { useVoiceNavigation } from '../hooks/useVoiceNavigation';
import './MainLayout.css';

export const MainLayout = () => {
  // Enable voice command navigation
  useVoiceNavigation();

  return (
    <div className="layout">
      <Sidebar />
      <div className="layout__content">
        <TopBar />
        <main>
          <Outlet />
        </main>
      </div>
      <ToastContainer />
      <VoiceOverlay />
    </div>
  );
};

