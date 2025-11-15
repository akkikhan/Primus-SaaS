import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useApplicationsStore } from '../state/useApplicationsStore';
import './ApplicationsPage.css';

export const ApplicationsPage = () => {
  const { applications, fetchApplications } = useApplicationsStore();

  useEffect(() => {
    void fetchApplications();
  }, [fetchApplications]);

  return (
    <section>
      <header className="apps__header">
        <div>
          <h1>Client Applications</h1>
          <p>Track all products consuming Primus modules.</p>
        </div>
        <button type="button">+ Register Application</button>
      </header>

      <div className="apps__grid">
        {applications.map(app => (
          <Link key={app.id} to={`/applications/${app.id}`} className="apps__card">
            <div className="apps__card-header">
              <h3>{app.name}</h3>
              <span className={`badge badge--${app.stack.toLowerCase()}`}>{app.stack}</span>
            </div>
            <p>{app.ownerEmail}</p>
            <div className="apps__card-meta">
              <span>Modules: {app.moduleCount}</span>
              <span>Client ID: {app.primusClientId}</span>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
};
