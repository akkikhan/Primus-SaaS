import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AppRoutes } from './routes/AppRoutes';
import { DashboardPage } from './pages/DashboardPage';
import { ApplicationsPage } from './pages/ApplicationsPage';
import Notifications from './pages/Notifications';
import { UpgradeManagerPage } from './pages/UpgradeManagerPage';

const App = () => (
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<DashboardPage />} />
      <Route path="applications" element={<ApplicationsPage />} />
      <Route path="notifications" element={<Notifications />} />
      <Route path="upgrade-manager" element={<UpgradeManagerPage />} />
    </Routes>
  </BrowserRouter>
);

export default App;
