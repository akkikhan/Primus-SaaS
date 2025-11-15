import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useApplicationsStore } from '../state/applicationsStore';
import { SkeletonCard } from '../components/Skeleton';
import './ApplicationsPage.css';

export const ApplicationsPage = () => {
  const { applications, fetchApplications, createApplication, deleteApplication, isLoading } = useApplicationsStore();
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({ name: '', clientId: '', clientSecret: '' });

  useEffect(() => {
    void fetchApplications();
  }, [fetchApplications]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createApplication(formData);
      setShowModal(false);
      setFormData({ name: '', clientId: '', clientSecret: '' });
    } catch (error) {
      // Error handled by store
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (window.confirm(`Delete application "${name}"? This cannot be undone.`)) {
      await deleteApplication(id);
    }
  };

  return (
    <section>
      <header className="apps__header">
        <div>
          <h1>Client Applications</h1>
          <p>Track all products consuming Primus modules.</p>
        </div>
        <button type="button" onClick={() => setShowModal(true)}>+ Register Application</button>
      </header>

      {isLoading ? (
        <div className="apps__grid">
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
          <SkeletonCard />
        </div>
      ) : (
      <div className="apps__grid">
        {applications.map(app => (
          <div key={app.id} className="apps__card">
            <Link to={`/applications/${app.id}`} className="apps__card-link">
              <div className="apps__card-header">
                <h3>{app.name}</h3>
              </div>
              <div className="apps__card-meta">
                <span>Modules: {app.applicationModules?.length || 0}</span>
                <span>Client ID: {app.clientId}</span>
              </div>
            </Link>
            <button 
              type="button" 
              className="apps__delete"
              onClick={(e) => {
                e.preventDefault();
                handleDelete(app.id, app.name);
              }}
            >
              Delete
            </button>
          </div>
        ))}
      </div>
      )}

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Register New Application</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label htmlFor="name">Application Name</label>
                <input
                  id="name"
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="clientId">Client ID</label>
                <input
                  id="clientId"
                  type="text"
                  value={formData.clientId}
                  onChange={(e) => setFormData({ ...formData, clientId: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="clientSecret">Client Secret</label>
                <input
                  id="clientSecret"
                  type="password"
                  value={formData.clientSecret}
                  onChange={(e) => setFormData({ ...formData, clientSecret: e.target.value })}
                  required
                />
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  );
};
