import { useEffect } from 'react';
import { StatsCard } from '../components/StatsCard';
import { useModulesStore } from '../state/modulesStore';
import { useApplicationsStore } from '../state/applicationsStore';
import { SkeletonStats } from '../components/Skeleton';
import './DashboardPage.css';

type LatestRelease = {
  moduleName: string;
  version: string;
  releasedAt?: string;
  isBreakingChange: boolean;
};

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
    (sum, app) => sum + (app.moduleCount || 0),
    0
  );
  const totalVersions = modules.reduce(
    (sum, mod) => sum + (mod.moduleVersions?.length || 0),
    0
  );

  const topModules = [...modules]
    .sort((a, b) => (b.usageCount || 0) - (a.usageCount || 0))
    .slice(0, 5);

  const keyApplications = [...applications]
    .sort((a, b) => (b.moduleCount || 0) - (a.moduleCount || 0))
    .slice(0, 5);

  const stackBreakdown = applications.reduce<Record<string, number>>((acc, app) => {
    if (!app.stack) {
      return acc;
    }
    acc[app.stack] = (acc[app.stack] || 0) + 1;
    return acc;
  }, {});
  const stackEntries = Object.entries(stackBreakdown).sort((a, b) => b[1] - a[1]);

  const latestReleases: LatestRelease[] = modules
    .reduce<LatestRelease[]>((acc, module) => {
      const versions = module.moduleVersions?.filter((version) => Boolean(version.releasedAt)) ?? [];
      if (!versions.length) {
        return acc;
      }

      const latest = [...versions].sort(
        (a, b) => new Date(b.releasedAt).getTime() - new Date(a.releasedAt).getTime()
      )[0];

      if (!latest) {
        return acc;
      }

      acc.push({
        moduleName: module.name,
        version: latest.version,
        releasedAt: latest.releasedAt,
        isBreakingChange: latest.isBreakingChange,
      });
      return acc;
    }, [])
    .sort((a, b) => new Date(b.releasedAt || 0).getTime() - new Date(a.releasedAt || 0).getTime())
    .slice(0, 4);

  const isLoading = modulesLoading || appsLoading;

  return (
    <div className="dashboard">
      <div className="dashboard__header">
        <div className="dashboard__title">
          <span className="dashboard__eyebrow">Executive summary</span>
          <h1>Platform Overview</h1>
          <p className="dashboard__lead">
            Unified visibility into modules, client applications, and release cadence for the Primus
            platform.
          </p>
        </div>
      </div>
      {isLoading ? (
        <div className="dashboard__grid">
          <SkeletonStats />
          <SkeletonStats />
          <SkeletonStats />
          <SkeletonStats />
        </div>
      ) : (
        <>
          <div className="dashboard__grid">
            <StatsCard title="Active Modules" value={totalModules} subtitle="Published to catalog" />
            <StatsCard title="Client Apps" value={totalApplications} subtitle="Registered" />
            <StatsCard title="Module Versions" value={totalVersions} subtitle="Total available" />
            <StatsCard
              title="Total Integrations"
              value={totalIntegrations}
              subtitle="Across applications"
            />
          </div>
          <div className="dashboard__insights">
            <section className="dashboard__panel dashboard__panel--table">
              <div className="dashboard__panel-heading">
                <div>
                  <span className="dashboard__panel-label">Usage</span>
                  <h2>Top Modules</h2>
                </div>
                <span className="dashboard__panel-meta">
                  {topModules.length} / {totalModules} tracked
                </span>
              </div>
              {topModules.length ? (
                <table className="dashboard__table">
                  <thead>
                    <tr>
                      <th>Module</th>
                      <th>Usage</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {topModules.map((module) => (
                      <tr key={module.id}>
                        <td>
                          <p className="dashboard__table-title">{module.name}</p>
                          <p className="dashboard__table-subtitle">{module.description}</p>
                        </td>
                        <td>{module.usageCount ?? 'N/A'}</td>
                        <td>
                          <span className="dashboard__badge">
                            {module.status?.replace(/([A-Z])/g, ' $1')?.trim() || 'Active'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className="dashboard__panel-description">
                  Module usage analytics will populate once telemetry is available.
                </p>
              )}
            </section>
            <section className="dashboard__panel dashboard__panel--table">
              <div className="dashboard__panel-heading">
                <div>
                  <span className="dashboard__panel-label">Applications</span>
                  <h2>Portfolio Overview</h2>
                </div>
                <span className="dashboard__panel-meta">{totalApplications} total</span>
              </div>
              <div className="dashboard__stack-grid">
                {stackEntries.slice(0, 4).map(([stack, count]) => (
                  <div key={stack} className="dashboard__stack-pill">
                    <strong>{stack}</strong>
                    <span>{count} apps</span>
                  </div>
                ))}
              </div>
              {keyApplications.length ? (
                <table className="dashboard__table">
                  <thead>
                    <tr>
                      <th>Application</th>
                      <th>Stack</th>
                      <th>Modules</th>
                    </tr>
                  </thead>
                  <tbody>
                    {keyApplications.map((app) => (
                      <tr key={app.id}>
                        <td>
                          <p className="dashboard__table-title">{app.name}</p>
                          <p className="dashboard__table-subtitle">{app.ownerEmail}</p>
                        </td>
                        <td>{app.stack || 'N/A'}</td>
                        <td>{app.moduleCount}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className="dashboard__panel-description">
                  Register applications to view adoption metrics.
                </p>
              )}
            </section>
          </div>
          <div className="dashboard__insights">
            <section className="dashboard__panel dashboard__panel--table">
              <div className="dashboard__panel-heading">
                <div>
                  <span className="dashboard__panel-label">Releases</span>
                  <h2>Recent Versions</h2>
                </div>
                <span className="dashboard__panel-meta">Chronological</span>
              </div>
              {latestReleases.length ? (
                <table className="dashboard__table">
                  <thead>
                    <tr>
                      <th>Module</th>
                      <th>Version</th>
                      <th>Released</th>
                    </tr>
                  </thead>
                  <tbody>
                    {latestReleases.map((release) => (
                      <tr key={`${release.moduleName}-${release.version}`}>
                        <td>{release.moduleName}</td>
                        <td>
                          <span
                            className={`dashboard__badge ${
                              release.isBreakingChange ? 'dashboard__badge--warning' : ''
                            }`}
                          >
                            {release.version}
                          </span>
                        </td>
                        <td>
                          {release.releasedAt
                            ? new Date(release.releasedAt).toLocaleDateString()
                            : 'N/A'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className="dashboard__panel-description">
                  Publish module versions to visualize release activity.
                </p>
              )}
            </section>
          </div>
        </>
      )}
    </div>
  );
};
