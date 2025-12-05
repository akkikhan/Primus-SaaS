import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';
import { ModulesPage } from '../pages/ModulesPage';
import { ApplicationsPage } from '../pages/ApplicationsPage';
import { ApplicationDetailsPage } from '../pages/ApplicationDetailsPage';
import { DocumentationPage } from '../pages/DocumentationPage';
import { UpgradeManagerPage } from '../pages/UpgradeManagerPage';
import Notifications from '../pages/Notifications';
import VoiceAssistantDemo from '../pages/VoiceAssistantDemo';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { MainLayout } from '../components/MainLayout';

export const AppRoutes = () => (
  <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route
      path="/"
      element={
        <ProtectedRoute>
          <MainLayout />
        </ProtectedRoute>
      }
    >
      <Route index element={<DashboardPage />} />
      <Route path="modules" element={<ModulesPage />} />
      <Route path="applications" element={<ApplicationsPage />} />
      <Route path="applications/:id" element={<ApplicationDetailsPage />} />
      <Route path="applications/:id/documentation" element={<DocumentationPage />} />
      <Route path="upgrade-manager" element={<UpgradeManagerPage />} />
      <Route path="notifications" element={<Notifications />} />
      <Route path="voice-demo" element={<VoiceAssistantDemo />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);

