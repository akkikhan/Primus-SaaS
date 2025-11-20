import { useCallback, useEffect, useState } from 'react';
import apiClient, { getErrorMessage } from '../services/apiClient';
import { useUIStore } from '../state/uiStore';
import './UpgradeManagerPage.css';

interface ModuleUpgrade {
  moduleId: number;
  moduleName: string;
  currentVersion: string;
  latestVersion: string;
  currentReleaseNotes: string;
  currentChangelog: string;
  latestReleaseNotes: string;
  latestChangelog: string;
  status: string;
  isBreakingChange: boolean;
}

interface AppUpgrade {
  applicationId: number;
  applicationName: string;
  stack: string;
  primusClientId: string;
  modules: ModuleUpgrade[];
}

export const UpgradeManagerPage = () => {
  const [upgrades, setUpgrades] = useState<AppUpgrade[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<ModuleUpgrade | null>(null);
  const [selectedApp, setSelectedApp] = useState<AppUpgrade | null>(null);
  const addToast = useUIStore((state) => state.addToast);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const response = await apiClient.get('/upgrade/overview');
      setUpgrades(response.data);
    } catch (err) {
      addToast('error', `Failed to load upgrades: ${getErrorMessage(err)}`);
    } finally {
      setLoading(false);
    }
  }, [addToast]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const handleUpgrade = async (app: AppUpgrade, module: ModuleUpgrade) => {
    try {
      await apiClient.post(`/upgrade/applications/${app.applicationId}/modules/${module.moduleId}/upgrade`);
      addToast('success', `Upgraded ${module.moduleName} on ${app.applicationName} to v${module.latestVersion}`);
      void loadData();
    } catch (err) {
      addToast('error', `Upgrade failed: ${getErrorMessage(err)}`);
    }
  };

  const totalUpgradable = upgrades.reduce((sum, app) => sum + app.modules.filter(m => m.status === 'UpdateAvailable').length, 0);

  return (
    <section className="upgrade-manager-page">
      <header>
        <div>
          <p className="eyebrow">System Management</p>
          <h1>Upgrade Manager</h1>
          <p>Upgrade modules across applications with one click, see breaking changes, and review changelogs.</p>
        </div>
        <div className="upgrade-summary">
          <div className="pill">{totalUpgradable} upgrades available</div>
        </div>
      </header>

      {loading ? (
        <div className="empty-state"><p>Loading upgrade data…</p></div>
      ) : (
        <div className="upgrade-grid">
          {upgrades.map(app => (
            <div key={app.applicationId} className="upgrade-card">
              <div className="upgrade-card__header">
                <div>
                  <h3>{app.applicationName}</h3>
                  <p className="muted">{app.stack} • {app.primusClientId}</p>
                </div>
              </div>
              <div className="upgrade-table">
                <div className="upgrade-table__head">
                  <span>Module</span>
                  <span>Current</span>
                  <span>Latest</span>
                  <span>Status</span>
                  <span>Actions</span>
                </div>
                {app.modules.map(module => (
                  <div key={module.moduleId} className="upgrade-table__row">
                    <span>{module.moduleName}</span>
                    <span>v{module.currentVersion}</span>
                    <span>v{module.latestVersion}</span>
                    <span className={module.status === 'UpdateAvailable' ? 'status-warning' : 'status-ok'}>
                      {module.status === 'UpdateAvailable' ? 'Update Available' : 'Up-to-date'}
                      {module.isBreakingChange && <small className="breaking-flag">⚠ Breaking</small>}
                    </span>
                    <span className="upgrade-actions">
                      <button
                        type="button"
                        onClick={() => { setSelected(module); setSelectedApp(app); }}
                      >
                        View Changelog
                      </button>
                      <button
                        type="button"
                        className="btn-primary"
                        disabled={module.status !== 'UpdateAvailable'}
                        onClick={() => handleUpgrade(app, module)}
                      >
                        One-click Upgrade
                      </button>
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {selected && selectedApp && (
        <div className="modal-overlay" onClick={() => { setSelected(null); setSelectedApp(null); }}>
          <div className="modal modal-large" onClick={(e) => e.stopPropagation()}>
            <h2>{selected.moduleName} changelog</h2>
            <p className="muted">Application: {selectedApp.applicationName}</p>

            <div className="changelog-section">
              <h4>Current version (v{selected.currentVersion})</h4>
              <p>{selected.currentReleaseNotes || 'No notes provided'}</p>
              {selected.currentChangelog && (
                <pre className="changelog-text">{selected.currentChangelog}</pre>
              )}
            </div>

            <div className="changelog-section">
              <h4>Latest version (v{selected.latestVersion})</h4>
              <p>{selected.latestReleaseNotes || 'No notes provided'}</p>
              {selected.latestChangelog && (
                <pre className="changelog-text">{selected.latestChangelog}</pre>
              )}
              {selected.isBreakingChange && <p className="breaking-flag">⚠ Breaking change</p>}
            </div>

            <div className="modal-actions">
              <button type="button" onClick={() => { setSelected(null); setSelectedApp(null); }}>Close</button>
              <button
                type="button"
                className="btn-primary"
                disabled={selected.status !== 'UpdateAvailable'}
                onClick={() => {
                  if (selectedApp) {
                    handleUpgrade(selectedApp, selected);
                    setSelected(null);
                    setSelectedApp(null);
                  }
                }}
              >
                Upgrade to v{selected.latestVersion}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
};
