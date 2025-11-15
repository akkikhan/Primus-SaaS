import { useParams } from 'react-router-dom';
import { useDocumentation } from '../hooks/useDocumentation';
import jsPDF from 'jspdf';
import './DocumentationPage.css';

export const DocumentationPage = () => {
  const { id } = useParams<{ id: string }>();
  const { documentation, loading, error } = useDocumentation(id);

  // Export to PDF
  const exportToPDF = () => {
    if (!documentation) return;

    const doc = new jsPDF();
    let yPos = 20;
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 20;
    const maxWidth = pageWidth - 2 * margin;

    // Title
    doc.setFontSize(20);
    doc.text(documentation.applicationName, margin, yPos);
    yPos += 10;

    // Metadata
    doc.setFontSize(10);
    doc.text(`Client ID: ${documentation.primusClientId}`, margin, yPos);
    yPos += 6;
    doc.text(`Stack: ${documentation.stack}`, margin, yPos);
    yPos += 6;
    doc.text(`Generated: ${new Date(documentation.generatedAt).toLocaleString()}`, margin, yPos);
    yPos += 15;

    // Modules
    documentation.modules.forEach((module) => {
      // Check if we need a new page
      if (yPos > 250) {
        doc.addPage();
        yPos = 20;
      }

      doc.setFontSize(16);
      doc.text(`${module.moduleName} v${module.version}`, margin, yPos);
      yPos += 10;

      if (module.releaseNotes) {
        doc.setFontSize(12);
        doc.text('Release Notes:', margin, yPos);
        yPos += 6;
        doc.setFontSize(10);
        const notes = doc.splitTextToSize(module.releaseNotes, maxWidth);
        doc.text(notes, margin, yPos);
        yPos += notes.length * 5 + 5;
      }

      doc.setFontSize(12);
      doc.text('Integration Steps:', margin, yPos);
      yPos += 6;
      doc.setFontSize(10);
      module.integrationSteps.forEach((step, idx) => {
        const stepText = doc.splitTextToSize(`${idx + 1}. ${step}`, maxWidth);
        doc.text(stepText, margin, yPos);
        yPos += stepText.length * 5 + 2;
      });
      yPos += 5;
    });

    doc.save(`${documentation.applicationName.replace(/\s+/g, '_')}_documentation.pdf`);
  };

  // Export to Markdown
  const exportToMarkdown = () => {
    if (!documentation) return;

    let markdown = `# ${documentation.applicationName}\n\n`;
    markdown += `**Client ID:** ${documentation.primusClientId}\n\n`;
    markdown += `**Stack:** ${documentation.stack}\n\n`;
    markdown += `**Generated:** ${new Date(documentation.generatedAt).toLocaleString()}\n\n`;
    markdown += `---\n\n`;

    documentation.modules.forEach((module) => {
      markdown += `## ${module.moduleName} v${module.version}\n\n`;

      if (module.isBreakingChange) {
        markdown += `⚠️ **Breaking Change**\n\n`;
      }

      if (module.releaseNotes) {
        markdown += `### Release Notes\n\n${module.releaseNotes}\n\n`;
      }

      markdown += `### Integration Steps\n\n`;
      module.integrationSteps.forEach((step, idx) => {
        markdown += `${idx + 1}. ${step}\n`;
      });
      markdown += `\n`;

      markdown += `### Code Snippets\n\n`;
      Object.entries(module.codeSnippets).forEach(([filename, code]) => {
        markdown += `#### ${filename}\n\n\`\`\`\n${code}\n\`\`\`\n\n`;
      });

      markdown += `---\n\n`;
    });

    const blob = new Blob([markdown], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${documentation.applicationName.replace(/\s+/g, '_')}_documentation.md`;
    a.click();
    URL.revokeObjectURL(url);
  };

  // Export to JSON
  const exportToJSON = () => {
    if (!documentation) return;

    const jsonString = JSON.stringify(documentation, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${documentation.applicationName.replace(/\s+/g, '_')}_documentation.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (loading) {
    return (
      <section className="documentation-page">
        <div className="loading-state">
          <p>Loading documentation...</p>
        </div>
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
          <button type="button" className="btn primary" onClick={exportToPDF}>
            Export PDF
          </button>
          <button type="button" className="btn secondary" onClick={exportToMarkdown}>
            Export Markdown
          </button>
          <button type="button" className="btn secondary" onClick={exportToJSON}>
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
