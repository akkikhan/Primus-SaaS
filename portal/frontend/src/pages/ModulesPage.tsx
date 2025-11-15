import { useEffect } from 'react';
import { useModulesStore } from '../state/useModulesStore';
import './ModulesPage.css';

export const ModulesPage = () => {
  const { modules, fetchModules } = useModulesStore();

  useEffect(() => {
    void fetchModules();
  }, [fetchModules]);

  return (
    <section>
      <header className="modules__header">
        <div>
          <h1>Modules</h1>
          <p>Manage reusable backend capabilities across integrated apps.</p>
        </div>
        <button type="button">+ New Module</button>
      </header>

      <div className="modules__table">
        <div className="modules__table-head">
          <span>Name</span>
          <span>Description</span>
          <span>Latest Version</span>
          <span>Published</span>
        </div>
        {modules.map(module => (
          <div key={module.id} className="modules__row">
            <span>{module.name}</span>
            <span>{module.description}</span>
            <span>{module.latestVersion}</span>
            <span>{module.releasedAt}</span>
          </div>
        ))}
      </div>
    </section>
  );
};
