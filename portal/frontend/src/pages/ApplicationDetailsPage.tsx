import { useParams, Link, Outlet } from 'react-router-dom';
import { useApplicationsStore } from '../state/applicationsStore';
import { useModulesStore } from '../state/modulesStore';
import { useEffect, useState } from 'react';
import './ApplicationDetailsPage.css';

export const ApplicationDetailsPage = () => {
  const { id } = useParams();
  const { currentApplication, fetchApplication, addModule, removeModule, isLoading } = useApplicationsStore();
  const { modules, fetchModules } = useModulesStore();
  const [showAddModule, setShowAddModule] = useState(false);
  const [selectedModuleId, setSelectedModuleId] = useState<number | null>(null);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);

  useEffect(() => {
    if (id) {
      void fetchApplication(Number(id));
      void fetchModules();
    }
  }, [id, fetchApplication, fetchModules]);

  if (isLoading || !currentApplication) {
    return <p>Loading application…</p>;
  }

  const handleAddModule = async () => {
    if (selectedModuleId && selectedVersionId && id) {
      try {
        await addModule(Number(id), selectedModuleId, selectedVersionId);
        setShowAddModule(false);
        setSelectedModuleId(null);
        setSelectedVersionId(null);
      } catch (error) {
        // Error handled by store
      }
    }
  };

  const handleRemoveModule = async (moduleId: number, moduleName: string) => {
    if (window.confirm(`Remove ${moduleName} from this application?`)) {
      await removeModule(Number(id), moduleId);
    }
  };

  const selectedModule = modules.find(m => m.id === selectedModuleId);

  return (
    <section className="app-detail">
      <header>
        <div>
          <h1>{currentApplication.name}</h1>
          <p>Client ID: {currentApplication.clientId}</p>
        </div>
        <div className="app-detail__actions">
          <button type="button" onClick={() => setShowAddModule(true)}>+ Add Module</button>
          <Link to={`/applications/${currentApplication.id}/documentation`} className="btn">
            View Documentation
          </Link>
        </div>
      </header>

      <div className="app-detail__grid">
        <div className="app-detail__panel">
          <h3>Integrated Modules</h3>
          {currentApplication.applicationModules?.length === 0 ? (
            <p>No modules integrated yet.</p>
          ) : (
            <ul>
              {currentApplication.applicationModules?.map(appModule => (
                <li key={appModule.moduleId}>
                  <div>
                    <strong>{appModule.moduleName || `Module ${appModule.moduleId}`}</strong>
                    <span>v{appModule.versionNumber || 'unknown'}</span>
                  </div>
                  <p>Integrated on {new Date(appModule.integratedAt).toLocaleDateString()}</p>
                  <button 
                    type="button" 
                    onClick={() => handleRemoveModule(appModule.moduleId, appModule.moduleName || 'Module')}
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="app-detail__panel">
          <h3>Application Info</h3>
          <p><strong>Created:</strong> {new Date(currentApplication.createdAt).toLocaleDateString()}</p>
          <p><strong>Total Modules:</strong> {currentApplication.applicationModules?.length || 0}</p>
        </div>
      </div>

      {showAddModule && (
        <div className="modal-overlay" onClick={() => setShowAddModule(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Add Module to Application</h2>
            <div className="form-group">
              <label htmlFor="module">Select Module</label>
              <select
                id="module"
                value={selectedModuleId || ''}
                onChange={(e) => {
                  setSelectedModuleId(Number(e.target.value));
                  setSelectedVersionId(null);
                }}
              >
                <option value="">Choose a module...</option>
                {modules.map(module => (
                  <option key={module.id} value={module.id}>{module.name}</option>
                ))}
              </select>
            </div>

            {selectedModule && (
              <div className="form-group">
                <label htmlFor="version">Select Version</label>
                <select
                  id="version"
                  value={selectedVersionId || ''}
                  onChange={(e) => setSelectedVersionId(Number(e.target.value))}
                >
                  <option value="">Choose a version...</option>
                  {selectedModule.moduleVersions?.map(version => (
                    <option key={version.id} value={version.id}>
                      {version.versionNumber} {version.isBreakingChange ? '⚠️ Breaking' : ''}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="modal-actions">
              <button type="button" onClick={() => setShowAddModule(false)}>Cancel</button>
              <button 
                type="button" 
                onClick={handleAddModule}
                disabled={!selectedModuleId || !selectedVersionId}
              >
                Add Module
              </button>
            </div>
          </div>
        </div>
      )}

      <Outlet />
    </section>
  );
};
