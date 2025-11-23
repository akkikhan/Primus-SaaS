import { useEffect, useState } from 'react';
import { useModulesStore } from '../state/modulesStore';
import { SkeletonTable } from '../components/Skeleton';
import './ModulesPage.css';

export const ModulesPage = () => {
  const { modules, fetchModules, createModule, deleteModule, addVersion, isLoading } = useModulesStore();
  const [showModuleModal, setShowModuleModal] = useState(false);
  const [showVersionModal, setShowVersionModal] = useState<number | null>(null);
  const [moduleForm, setModuleForm] = useState({ name: '', description: '', moduleKey: '' });
  const [versionForm, setVersionForm] = useState({
    version: '',
    releaseNotes: '',
    changelog: '',
    demoCode: '',
    isBreakingChange: false,
    notifyClients: false
  });

  useEffect(() => {
    void fetchModules();
  }, [fetchModules]);

  const handleModuleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createModule(moduleForm);
      setShowModuleModal(false);
      setModuleForm({ name: '', description: '', moduleKey: '' });
    } catch {
      // Error handled by store
    }
  };

  const handleVersionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (showVersionModal) {
      try {
        const versionPayload = {
          ...versionForm,
          supportedStacks: [],
          releasedAt: new Date().toISOString(),
        };
        await addVersion(showVersionModal, versionPayload);
        setShowVersionModal(null);
        setVersionForm({ version: '', releaseNotes: '', changelog: '', demoCode: '', isBreakingChange: false, notifyClients: false });
      } catch {
        // Error handled by store
      }
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (window.confirm(`Delete module "${name}"? This will also remove it from all applications.`)) {
      await deleteModule(id);
    }
  };

  const formatDate = (value?: string) => {
    if (!value) return 'N/A';
    return new Date(value).toLocaleDateString();
  };

  return (
    <section>
      <header className="modules__header">
        <div>
          <h1>Modules</h1>
          <p>Manage reusable backend capabilities across integrated apps.</p>
        </div>
        <button type="button" onClick={() => setShowModuleModal(true)}>+ New Module</button>
      </header>

      {isLoading ? (
        <SkeletonTable rows={5} />
      ) : (
        <div className="modules__table">
          <div className="modules__table-head">
            <span>Name</span>
            <span>Description</span>
            <span>Latest Version</span>
            <span>Published</span>
            <span>Status</span>
            <span>Apps Using</span>
            <span>Actions</span>
          </div>
          {modules.map(module => (
            <div key={module.id} className="modules__row">
              <span>{module.name}</span>
              <span>{module.description}</span>
              <span>{module.latestVersion || 'N/A'}</span>
              <span>{formatDate(module.latestReleasedAt)}</span>
              <span>{module.status || 'Active'}</span>
              <span>{module.usageCount ?? 0}</span>
              <span className="modules__actions">
                <button type="button" onClick={() => setShowVersionModal(module.id)}>+ Version</button>
                <button type="button" onClick={() => handleDelete(module.id, module.name)}>Delete</button>
              </span>
            </div>
          ))}
        </div>
      )}

      {showModuleModal && (
        <div className="modal-overlay" onClick={() => setShowModuleModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Create New Module</h2>
            <form onSubmit={handleModuleSubmit}>
              <div className="form-group">
                <label htmlFor="name">Module Name</label>
                <input
                  id="name"
                  type="text"
                  value={moduleForm.name}
                  onChange={(e) => setModuleForm({ ...moduleForm, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="moduleKey">
                  Module Key <span className="hint">(system readable)</span>
                </label>
                <input
                  id="moduleKey"
                  type="text"
                  value={moduleForm.moduleKey}
                  onChange={(e) => setModuleForm({ ...moduleForm, moduleKey: e.target.value })}
                  placeholder="identity-validator"
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="description">Description</label>
                <textarea
                  id="description"
                  value={moduleForm.description}
                  onChange={(e) => setModuleForm({ ...moduleForm, description: e.target.value })}
                  required
                />
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowModuleModal(false)}>Cancel</button>
                <button type="submit">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showVersionModal && (
        <div className="modal-overlay" onClick={() => setShowVersionModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Add New Version</h2>
            <form onSubmit={handleVersionSubmit}>
              <div className="form-group">
                <label htmlFor="versionNumber">Version Number</label>
                <input
                  id="versionNumber"
                  type="text"
                  placeholder="e.g., 1.2.0"
                  value={versionForm.version}
                  onChange={(e) => setVersionForm({ ...versionForm, version: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="releaseNotes">Release Notes</label>
                <textarea
                  id="releaseNotes"
                  value={versionForm.releaseNotes}
                  onChange={(e) => setVersionForm({ ...versionForm, releaseNotes: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="changelog">Changelog</label>
                <textarea
                  id="changelog"
                  placeholder="Detailed changelog for this version..."
                  value={versionForm.changelog}
                  onChange={(e) => setVersionForm({ ...versionForm, changelog: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label htmlFor="demoCode">Demo Code</label>
                <textarea
                  id="demoCode"
                  placeholder="Example code demonstrating new features..."
                  value={versionForm.demoCode}
                  onChange={(e) => setVersionForm({ ...versionForm, demoCode: e.target.value })}
                  rows={8}
                />
              </div>
              <div className="form-group form-group--checkbox">
                <label>
                  <input
                    type="checkbox"
                    checked={versionForm.isBreakingChange}
                    onChange={(e) => setVersionForm({ ...versionForm, isBreakingChange: e.target.checked })}
                  />
                  Breaking Change
                </label>
              </div>
              <div className="form-group form-group--checkbox">
                <label>
                  <input
                    type="checkbox"
                    checked={versionForm.notifyClients}
                    onChange={(e) => setVersionForm({ ...versionForm, notifyClients: e.target.checked })}
                  />
                  Notify Clients
                  <span className="hint"> (Send email notifications to all apps using this module)</span>
                </label>
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowVersionModal(null)}>Cancel</button>
                <button type="submit">Add Version</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  );
};
