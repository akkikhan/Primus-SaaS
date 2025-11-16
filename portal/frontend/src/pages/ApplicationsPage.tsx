import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useApplicationsStore } from '../state/applicationsStore';
import { SkeletonCard } from '../components/Skeleton';
import './ApplicationsPage.css';

interface ApplicationFormData {
  name: string;
  stack: string;
  description: string;
}

export const ApplicationsPage = () => {
  const { applications, fetchApplications, createApplication, deleteApplication, isLoading } = useApplicationsStore();
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState<ApplicationFormData>({
    name: '',
    stack: '',
    description: '',
  });

  useEffect(() => {
    void fetchApplications();
  }, [fetchApplications]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createApplication(formData);
      setShowModal(false);
      setFormData({
        name: '',
        stack: '',
        description: '',
      });
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
                <span>Modules: {app.moduleCount || 0}</span>
                <span>Primus ID: {app.primusClientId}</span>
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
                  placeholder="My Application"
                  required
                />
              </div>
            <div className="form-group">
                <label htmlFor="stack">Technology Stack</label>
                <select
                  id="stack"
                  value={formData.stack}
                  onChange={(e) => setFormData({ ...formData, stack: e.target.value })}
                  required
                >
                  <option value="" disabled hidden>Select stack</option>
                  <option value="NodeJS">Node.js (Express)</option>
                  <option value="NodeJS-Nest">Node.js (NestJS)</option>
                  <option value="DotNet">.NET 8 Web API</option>
                  <option value="TypeScriptLib">TypeScript Library</option>
                  <option value="Python">Python FastAPI (preview)</option>
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="description">Description (Optional)</label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Brief description of your application"
                  rows={3}
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
