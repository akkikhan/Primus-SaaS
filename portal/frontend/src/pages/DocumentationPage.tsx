import { useParams } from 'react-router-dom';
import { useDocumentation } from '../hooks/useDocumentation';
import { generatePDF, generateMarkdown, generateJSON } from '../services/docGenerator';
import { SkeletonDocumentation } from '../components/Skeleton';
import './DocumentationPage.css';

export const DocumentationPage = () => {
  const { id } = useParams<{ id: string }>();
  const { documentation, loading, error } = useDocumentation(id);

  // ... export functions

  if (loading) {
    return (
      <section className="documentation-page">
        <SkeletonDocumentation />
      </section>
    );
  }

  if (error) {
    return (
      <section className="documentation-page">
        <div className="error-state">
          <h2>Error Loading Documentation</h2>
          <p>{error}</p>
        </div>
      </section>
    );
  }

  if (!documentation) {
    return (
      <section className="documentation-page">
        <div className="empty-state">
          <h2>No Documentation Available</h2>
          <p>Please select an application to view its documentation.</p>
        </div>
      </section>
    );
  }

  return (
    <section className="documentation-page">
      <header>
        <div>
          <p className="eyebrow">Application Documentation</p>
          <h1>{documentation.applicationName}</h1>
          <p>
            Client ID: <strong>{documentation.primusClientId}</strong> | Stack: <strong>{documentation.stack}</strong> | Generated: {new Date(documentation.generatedAt).toLocaleString()}
          </p>
        </div>
        <div className="actions">
          <button type="button" className="btn primary" onClick={() => generatePDF(documentation)}>
            Export PDF
          </button>
          <button type="button" className="btn secondary" onClick={() => generateMarkdown(documentation)}>
            Export Markdown
          </button>
          <button type="button" className="btn secondary" onClick={() => generateJSON(documentation)}>
            Export JSON
          </button>
        </div>
      </header>

      <div className="documentation-content">
        {documentation.modules.map((module, idx) => (
          <div key={idx} className="module-section">
            <div className="module-header">
              <h2>{module.moduleName}</h2>
              <span className="version-badge">
                v{module.version}
                {module.isBreakingChange && <span className="breaking-change"> ⚠️ Breaking</span>}
              </span>
            </div>

            {module.releaseNotes && (
              <div className="release-notes">
                <h3>Release Notes</h3>
                <p>{module.releaseNotes}</p>
              </div>
            )}

            <div className="integration-steps">
              <h3>Integration Steps</h3>
              <ol>
                {module.integrationSteps.map((step, stepIdx) => (
                  <li key={stepIdx}>{step}</li>
                ))}
              </ol>
            </div>

            <div className="code-snippets">
              <h3>Code Snippets</h3>
              {Object.entries(module.codeSnippets).map(([filename, code]) => (
                <div key={filename} className="code-snippet">
                  <h4>{filename}</h4>
                  <pre>
                    <code>{code}</code>
                  </pre>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
};
