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
    versionNumber: '', 
    releaseNotes: '', 
    isBreakingChange: false 
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
    } catch (error) {
      // Error handled by store
    }
  };

  const handleVersionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (showVersionModal) {
      try {
        const versionPayload = {
          ...versionForm,
          releasedAt: new Date().toISOString(),
        };
        await addVersion(showVersionModal, versionPayload);
        setShowVersionModal(null);
        setVersionForm({ versionNumber: '', releaseNotes: '', isBreakingChange: false });
      } catch (error) {
        // Error handled by store
      }
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (window.confirm(`Delete module "${name}"? This will also remove it from all applications.`)) {
      await deleteModule(id);
    }
  };

  const getLatestVersion = (module: any) => {
    if (!module.moduleVersions?.length) return 'N/A';
    return module.moduleVersions[module.moduleVersions.length - 1].versionNumber;
  };

  const getPublishedDate = (module: any) => {
    if (!module.moduleVersions?.length) return 'N/A';
    const date = new Date(module.moduleVersions[0].releasedAt);
    return date.toLocaleDateString();
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
          <span>Actions</span>
        </div>
        {modules.map(module => (
          <div key={module.id} className="modules__row">
            <span>{module.name}</span>
            <span>{module.description}</span>
            <span>{getLatestVersion(module)}</span>
            <span>{getPublishedDate(module)}</span>
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
                  value={versionForm.versionNumber}
                  onChange={(e) => setVersionForm({ ...versionForm, versionNumber: e.target.value })}
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
                <label>
                  <input
                    type="checkbox"
                    checked={versionForm.isBreakingChange}
                    onChange={(e) => setVersionForm({ ...versionForm, isBreakingChange: e.target.checked })}
                  />
                  Breaking Change
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
