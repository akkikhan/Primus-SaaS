import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useApplicationsStore } from '../state/applicationsStore';
import { SkeletonCard } from '../components/Skeleton';
import './ApplicationsPage.css';

interface ApplicationFormData {
  name: string;
  stack: string;
  description: string;
  clientEmail: string;
}

export const ApplicationsPage = () => {
  const { applications, fetchApplications, createApplication, deleteApplication, isLoading } = useApplicationsStore();
  const [showModal, setShowModal] = useState(false);
  const [newCredentials, setNewCredentials] = useState<{ clientId: string } | null>(null);
  const [formData, setFormData] = useState<ApplicationFormData>({
    name: '',
    stack: '',
    description: '',
    clientEmail: '',
  });
  const stackInfo: Record<string, { label: string; tone: string }> = {
    DotNet: { label: '.NET', tone: 'tone-dotnet' },
    NodeJS: { label: 'Node.js', tone: 'tone-node' },
    'NodeJS-Nest': { label: 'NestJS', tone: 'tone-node' },
    TypeScriptLib: { label: 'TypeScript Library', tone: 'tone-ts' },
    Python: { label: 'Python', tone: 'tone-python' },
    'Python-FastAPI': { label: 'FastAPI', tone: 'tone-python' }
  };

  useEffect(() => {
    void fetchApplications();
  }, [fetchApplications]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const created = await createApplication(formData);
      if (created?.primusClientId) {
        setNewCredentials({
          clientId: created.primusClientId
        });
      }
      setShowModal(false);
      setFormData({
        name: '',
        stack: '',
        description: '',
        clientEmail: '',
      });
    } catch {
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
                  <div>
                    <p className="apps__eyebrow">Registered app</p>
                    <h3>{app.name}</h3>
                  </div>
                  <span className={`stack-chip ${stackInfo[app.stack]?.tone ?? 'tone-neutral'}`}>
                    <span className="stack-dot" />
                    {stackInfo[app.stack]?.label ?? app.stack}
                  </span>
                </div>
                <p className="apps__card-desc">
                  {app.description?.trim() || 'No description added yet.'}
                </p>
                <div className="apps__card-meta">
                  <div className="meta-block">
                    <span className="meta-label">Modules</span>
                    <span className="meta-value">{app.moduleCount || 0}</span>
                  </div>
                  <div className="meta-block">
                    <span className="meta-label">Primus ID</span>
                    <span className="meta-value code">{app.primusClientId}</span>
                  </div>
                </div>
              </Link>
              <div className="apps__card-actions">
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
                <span className="apps__secondary-action">View details</span>
              </div>
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
              <div className="form-group">
                <label htmlFor="clientEmail">Client Email (for credentials & docs)</label>
                <input
                  id="clientEmail"
                  type="email"
                  value={formData.clientEmail}
                  onChange={(e) => setFormData({ ...formData, clientEmail: e.target.value })}
                  placeholder="client@example.com"
                />
                <small className="form-help">
                  We'll send the App ID and documentation link to this address.
                </small>
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {newCredentials && (
        <div className="modal-overlay" onClick={() => setNewCredentials(null)}>
          <div className="modal credentials-modal" onClick={(e) => e.stopPropagation()}>
            <h2>Application Identifier</h2>
            <p className="credentials-hint">
              Copy this ID now. Use it in generated docs/snippets as the audience value.
            </p>
            <div className="credentials-field">
              <label>Primus App ID</label>
              <div className="credentials-value">
                <code>{newCredentials.clientId}</code>
                <button
                  type="button"
                  onClick={() => navigator.clipboard.writeText(newCredentials.clientId)}
                >
                  Copy
                </button>
              </div>
            </div>
            <div className="modal-actions">
              <button type="button" onClick={() => setNewCredentials(null)}>
                Done
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
};
