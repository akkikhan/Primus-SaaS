import { useParams, Link, Outlet } from 'react-router-dom';
import { useApplicationDetailsStore } from '../state/useApplicationDetailsStore';
import { useEffect } from 'react';
import './ApplicationDetailsPage.css';

export const ApplicationDetailsPage = () => {
  const { id } = useParams();
  const { application, fetchApplication } = useApplicationDetailsStore();

  useEffect(() => {
    if (id) {
      void fetchApplication(Number(id));
    }
  }, [id, fetchApplication]);

  if (!application) {
    return <p>Loading application…</p>;
  }

  return (
    <section className="app-detail">
      <header>
        <div>
          <h1>{application.name}</h1>
          <p>Client ID: {application.primusClientId}</p>
        </div>
        <Link to={`/applications/${application.id}/documentation`} className="btn">
          View Documentation
        </Link>
      </header>

      <div className="app-detail__grid">
        <div className="app-detail__panel">
          <h3>Integrated Modules</h3>
          <ul>
            {application.integratedModules.map(module => (
              <li key={module.moduleId}>
                <div>
                  <strong>{module.moduleName}</strong>
                  <span>v{module.version}</span>
                </div>
                <p>{module.configSummary}</p>
              </li>
            ))}
          </ul>
        </div>

        <div className="app-detail__panel">
          <h3>Recent Activity</h3>
          <p>Activity feed placeholder. Hook into backend audit log later.</p>
        </div>
      </div>

      <Outlet />
    </section>
  );
};
