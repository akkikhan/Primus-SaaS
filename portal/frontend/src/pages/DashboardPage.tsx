import { useEffect } from 'react';
import { StatsCard } from '../components/StatsCard';
import { useDashboardStore } from '../state/useDashboardStore';
import './DashboardPage.css';

export const DashboardPage = () => {
  const { stats, fetchStats } = useDashboardStore();

  useEffect(() => {
    void fetchStats();
  }, [fetchStats]);

  return (
    <div className="dashboard">
      <h1>Platform Overview</h1>
      <div className="dashboard__grid">
        <StatsCard title="Active Modules" value={stats.modules} subtitle="Published to catalog" />
        <StatsCard title="Client Apps" value={stats.applications} subtitle="Registered" />
        <StatsCard title="Pending Updates" value={stats.updates} subtitle="Require review" />
        <StatsCard title="Total Integrations" value={stats.integrations} subtitle="Across apps" />
      </div>
    </div>
  );
};
