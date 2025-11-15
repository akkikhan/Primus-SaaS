import { useEffect } from 'react';
import { StatsCard } from '../components/StatsCard';
import { useModulesStore } from '../state/modulesStore';
import { useApplicationsStore } from '../state/applicationsStore';
import { SkeletonStats } from '../components/Skeleton';
import './DashboardPage.css';

export const DashboardPage = () => {
  const { modules, fetchModules, isLoading: modulesLoading } = useModulesStore();
  const { applications, fetchApplications, isLoading: appsLoading } = useApplicationsStore();

  useEffect(() => {
    void fetchModules();
    void fetchApplications();
  }, [fetchModules, fetchApplications]);

  // Calculate stats from real data
  const totalModules = modules.length;
  const totalApplications = applications.length;
  const totalIntegrations = applications.reduce(
    (sum, app) => sum + (app.integratedModules?.length || 0),
    0
  );
  const totalVersions = modules.reduce(
    (sum, mod) => sum + (mod.moduleVersions?.length || 0),
    0
  );

  const isLoading = modulesLoading || appsLoading;

  return (
    <div className="dashboard">
      <h1>Platform Overview</h1>
      {isLoading ? (
        <div className="dashboard__grid">
          <SkeletonStats />
          <SkeletonStats />
          <SkeletonStats />
          <SkeletonStats />
        </div>
      ) : (
      <div className="dashboard__grid">
        <StatsCard title="Active Modules" value={totalModules} subtitle="Published to catalog" />
        <StatsCard title="Client Apps" value={totalApplications} subtitle="Registered" />
        <StatsCard title="Module Versions" value={totalVersions} subtitle="Total available" />
        <StatsCard title="Total Integrations" value={totalIntegrations} subtitle="Across apps" />
      </div>
      )}
    </div>
  );
};
