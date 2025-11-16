import { useParams, Link, Outlet, useNavigate } from 'react-router-dom';
import { useApplicationsStore } from '../state/applicationsStore';
import { useModulesStore } from '../state/modulesStore';
import { useEffect, useState } from 'react';
import apiClient from '../services/apiClient';
import './ApplicationDetailsPage.css';

export const ApplicationDetailsPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { currentApplication, fetchApplication, addModule, removeModule, changeModuleVersion, updateApplication, isLoading } = useApplicationsStore();
  const { modules, fetchModules } = useModulesStore();
  const [showAddModule, setShowAddModule] = useState(false);
  const [selectedModuleId, setSelectedModuleId] = useState<number | null>(null);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [copiedText, setCopiedText] = useState<string>('');
  const [showChangeVersion, setShowChangeVersion] = useState(false);
  const [changingModuleId, setChangingModuleId] = useState<number | null>(null);
  const [newVersion, setNewVersion] = useState<string>('');
  const [showChangelog, setShowChangelog] = useState(false);
  const [changelogModule, setChangelogModule] = useState<any>(null);
  const [showEditApp, setShowEditApp] = useState(false);
  const [editForm, setEditForm] = useState({ name: '', stack: '', description: '' });
  const stackIcons: Record<string, string> = {
    DotNet: '🟣 .NET',
    NodeJS: '🟢 Node.js',
    'NodeJS-Nest': '🟢 NestJS',
    TypeScriptLib: '🔵 TS',
    Python: '🟠 Python',
    'Python-FastAPI': '🟠 FastAPI'
  };

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

  const handleOpenChangeVersion = (moduleId: number, currentVersion: string) => {
    setChangingModuleId(moduleId);
    setNewVersion(currentVersion);
    setShowChangeVersion(true);
  };

  const handleChangeVersion = async () => {
    if (changingModuleId && newVersion && id) {
      try {
        await changeModuleVersion(Number(id), changingModuleId, newVersion);
        setShowChangeVersion(false);
        setChangingModuleId(null);
        setNewVersion('');
      } catch (error) {
        // Error handled by store
      }
    }
  };

  const handleViewChangelog = (module: any) => {
    setChangelogModule(module);
    setShowChangelog(true);
  };

  const handleCopy = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    setCopiedText(label);
    setTimeout(() => setCopiedText(''), 2000);
  };

  const handleCopyAll = () => {
    const allDocs = `
# ${currentApplication.name} - Integration Documentation

## Install Command
${getInstallCommand()}

## Required Environment Variables
PRIMUS_CLIENT_ID=${currentApplication.primusClientId}
AZURE_TENANT_ID=<TENANT_ID>
AZURE_CLIENT_ID=<CLIENT_ID>
AZURE_AUDIENCE=api://<CLIENT_ID>

## Configuration
${getConfigTemplate()}

## Code Integration
${getCodeSnippet()}
    `.trim();
    handleCopy(allDocs, 'all-docs');
  };

  const handleDownloadPdf = async () => {
    if (!id) return;
    try {
      const response = await apiClient.get(`/documentation/${id}/pdf`, { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `${currentApplication.name}-PrimusDocs.pdf`);
      document.body.appendChild(link);
      link.click();
      link.parentNode?.removeChild(link);
      window.URL.revokeObjectURL(url);
      setCopiedText('pdf');
      setTimeout(() => setCopiedText(''), 2000);
    } catch (error) {
      console.error('Failed to download PDF', error);
    }
  };

  const selectedModule = modules.find(m => m.id === selectedModuleId);
  const changingModule = currentApplication?.integratedModules?.find(m => m.moduleId === changingModuleId);

  const openEditModal = () => {
    setEditForm({
      name: currentApplication.name,
      stack: currentApplication.stack,
      description: currentApplication.description ?? '',
    });
    setShowEditApp(true);
  };

  const handleEditSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    await updateApplication(Number(id), {
      name: editForm.name,
      stack: editForm.stack,
      description: editForm.description,
    });
    await fetchApplication(Number(id));
    setShowEditApp(false);
  };

  // Generate integration documentation based on stack
  const getInstallCommand = () => {
    switch (currentApplication.stack) {
      case 'DotNet':
        return 'dotnet add package PrimusSaaS.Identity.Validator';
      case 'NodeJS':
        return 'npm install @primus-saas/identity-validator';
      case 'NodeJS-Nest':
        return 'npm install @primus-saas/identity-validator';
      case 'TypeScriptLib':
        return 'npm install @primus-saas/identity-validator';
      case 'Python':
        return 'pip install primus-identity-validator';
      case 'Python-FastAPI':
        return 'pip install primus-identity-validator';
      default:
        return '';
    }
  };

  const getConfigTemplate = () => {
    const primusClientId = currentApplication.primusClientId;
    
    switch (currentApplication.stack) {
      case 'DotNet':
        return `{
  "Primus": {
    "ClientId": "${primusClientId}"
  },
  "Auth": {
    "Mode": "AzureAd",
    "AzureAd": {
      "TenantId": "<YOUR_TENANT_ID>",
      "ClientId": "<YOUR_AZURE_CLIENT_ID>",
      "Audience": "api://<YOUR_AZURE_CLIENT_ID>",
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0"
    }
  }
}`;
      case 'NodeJS':
      case 'NodeJS-Nest':
      case 'TypeScriptLib':
        return `{
  "Primus": {
    "ClientId": "${primusClientId}"
  },
  "Auth": {
    "Mode": "AzureAd",
    "AzureAd": {
      "TenantId": "<YOUR_TENANT_ID>",
      "ClientId": "<YOUR_AZURE_CLIENT_ID>",
      "Audience": "api://<YOUR_AZURE_CLIENT_ID>",
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0"
    }
  }
}`;
      case 'Python':
      case 'Python-FastAPI':
        return `{
  "Primus": {
    "ClientId": "${primusClientId}"
  },
  "Auth": {
    "Mode": "AzureAd",
    "AzureAd": {
      "TenantId": "<YOUR_TENANT_ID>",
      "ClientId": "<YOUR_AZURE_CLIENT_ID>",
      "Audience": "api://<YOUR_AZURE_CLIENT_ID>",
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0"
    }
  }
}`;
      default:
        return '';
    }
  };

  const getCodeSnippet = () => {
    const primusClientId = currentApplication.primusClientId;
    
    switch (currentApplication.stack) {
      case 'DotNet':
        return `// Program.cs
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentityValidator(options =>
{
    options.PrimusClientId = "${primusClientId}";
    options.AuthMode = "AzureAd";
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();`;
      case 'NodeJS':
        return `// server.ts
import { createIdentityValidator } from "@primus-saas/identity-validator";

const auth = createIdentityValidator({
  primusClientId: "${primusClientId}",
  mode: "AzureAd",
  azureAd: {
    tenantId: process.env.AZURE_TENANT!,
    clientId: process.env.AZURE_CLIENT!,
    audience: process.env.AZURE_AUD!
  }
});

// Use with Express
app.get("/api/me", auth.requireAuth, (req, res) => {
  res.json({ user: req.user });
});`;
      case 'NodeJS-Nest':
        return `// auth.module.ts
import { Module } from "@nestjs/common";
import { createIdentityValidator } from "@primus-saas/identity-validator";

const auth = createIdentityValidator({
  primusClientId: "${primusClientId}",
  mode: "AzureAd",
  azureAd: {
    tenantId: process.env.AZURE_TENANT!,
    clientId: process.env.AZURE_CLIENT!,
    audience: process.env.AZURE_AUD!
  }
});

@Module({
  providers: [
    {
      provide: 'AUTH_GUARD',
      useValue: auth.requireAuth
    }
  ],
  exports: ['AUTH_GUARD']
})
export class AuthModule {}

// user.controller.ts
@Controller('api/me')
export class UserController {
  @Get()
  @UseGuards('AUTH_GUARD')
  getUser(@Request() req) {
    return { user: req.user };
  }
}`;
      case 'TypeScriptLib':
        return `// validator.ts
import { createIdentityValidator } from "@primus-saas/identity-validator";

export const auth = createIdentityValidator({
  primusClientId: "${primusClientId}",
  mode: "AzureAd",
  azureAd: {
    tenantId: process.env.AZURE_TENANT!,
    clientId: process.env.AZURE_CLIENT!,
    audience: process.env.AZURE_AUD!
  }
});

// Usage in your code
const token = "Bearer eyJ...";
const result = await auth.validateToken(token);

if (result.isValid) {
  console.log("User:", result.user);
} else {
  console.error("Validation failed:", result.error);
}`;
      case 'Python':
        return `# app.py
from primus_identity_validator import createIdentityValidator

auth = createIdentityValidator(
    primus_client_id="${primusClientId}",
    mode="AzureAd",
    azure_ad={
        "tenant_id": os.getenv("AZURE_TENANT"),
        "client_id": os.getenv("AZURE_CLIENT"),
        "audience": os.getenv("AZURE_AUD")
    }
)

@app.route("/api/me")
@auth.require_auth
def get_user():
    return {"user": request.user}`;
      case 'Python-FastAPI':
        return `# main.py
from fastapi import FastAPI, Depends
from primus_identity_validator import createIdentityValidator

app = FastAPI()

auth = createIdentityValidator(
    primus_client_id="${primusClientId}",
    mode="AzureAd",
    azure_ad={
        "tenant_id": os.getenv("AZURE_TENANT"),
        "client_id": os.getenv("AZURE_CLIENT"),
        "audience": os.getenv("AZURE_AUD")
    }
)

@app.get("/api/me")
async def get_user(user=Depends(auth.require_auth)):
    return {"user": user}`;
      default:
        return '';
    }
  };

  return (
    <section className="app-detail">
      {/* Breadcrumb Navigation */}
      <nav className="breadcrumb">
        <Link to="/applications">Applications</Link>
        <span className="separator">›</span>
        <span className="current">{currentApplication.name}</span>
      </nav>

      {/* Header Section - Blueprint Format */}
      <header className="app-detail__blueprint-header">
        <div className="app-detail__header-primary">
          <h1>{currentApplication.name}</h1>
        </div>
        <div className="app-detail__header-info">
          <div className="header-field">
            <label>Primus Client ID:</label>
            <div className="value-with-copy">
              <code>{currentApplication.primusClientId}</code>
              <button 
                type="button" 
                className="copy-btn-inline"
                onClick={() => handleCopy(currentApplication.primusClientId, 'header-clientId')}
                title="Copy to clipboard"
              >
                {copiedText === 'header-clientId' ? '✓' : '📋'}
              </button>
            </div>
          </div>
          <div className="header-field">
            <label>Technology Stack:</label>
            <span className="badge">{stackIcons[currentApplication.stack] || currentApplication.stack}</span>
          </div>
          {currentApplication.description && (
            <div className="header-field">
              <label>Description:</label>
              <span>{currentApplication.description}</span>
            </div>
          )}
          <div className="header-actions">
            <button type="button" onClick={openEditModal}>
              Edit Application
            </button>
          </div>
        </div>
      </header>

      <div className="app-detail__grid">
        {/* Section 1: Application Information */}
        <div className="app-detail__panel app-detail__info-panel">
          <h2>Application Information</h2>
          <div className="info-grid">
            <div className="info-item">
              <label>Application Name</label>
              <div className="info-value">{currentApplication.name}</div>
            </div>
          <div className="info-item">
            <label>Technology Stack</label>
            <div className="info-value">
              <span className="badge">{stackIcons[currentApplication.stack] || currentApplication.stack}</span>
            </div>
          </div>
            <div className="info-item">
              <label>Primus Client ID</label>
              <div className="info-value copy-container">
                <code className="code-inline">{currentApplication.primusClientId}</code>
                <button 
                  type="button" 
                  className="copy-btn"
                  onClick={() => handleCopy(currentApplication.primusClientId, 'primusClientId')}
                  title="Copy to clipboard"
                >
                  {copiedText === 'primusClientId' ? '✓' : '📋'}
                </button>
              </div>
            </div>
            <div className="info-item">
              <label>Description</label>
              <div className="info-value">{currentApplication.description || 'No description provided'}</div>
            </div>
            <div className="info-item">
              <label>Created</label>
              <div className="info-value">{new Date(currentApplication.createdAt).toLocaleString()}</div>
            </div>
            {currentApplication.updatedAt && (
              <div className="info-item">
                <label>Last Updated</label>
                <div className="info-value">{new Date(currentApplication.updatedAt).toLocaleString()}</div>
              </div>
            )}
            <div className="info-item">
              <label>Integrated Modules</label>
              <div className="info-value">{currentApplication.integratedModules?.length || 0}</div>
            </div>
          </div>
        </div>

        {/* Section 2: Integrated Modules */}
        <div className="app-detail__panel">
          <div className="panel-heading">
            <h2>Integrated Modules & Versions</h2>
            <button type="button" onClick={() => setShowAddModule(true)}>
              + Assign Module
            </button>
          </div>
          {!currentApplication.integratedModules || currentApplication.integratedModules.length === 0 ? (
            <div className="empty-state">
              <p>No modules integrated yet.</p>
              <button type="button" onClick={() => setShowAddModule(true)}>
                + Integrate Your First Module
              </button>
            </div>
          ) : (
            <div className="modules-list">
              {currentApplication.integratedModules!.map(appModule => (
                <div key={appModule.moduleId} className="module-card">
                  <div className="module-header">
                    <div className="module-title-area">
                      <h3>{appModule.moduleName}</h3>
                    </div>
                  </div>
                  
                  <div className="module-version-info">
                    <div className="version-row">
                      <label>Current Version:</label>
                      <span className="version-text">v{appModule.version}</span>
                    </div>
                    <div className="version-row">
                      <label>Latest Available:</label>
                      <span className="version-text">v{appModule.latestVersion}</span>
                    </div>
                    <div className="version-row">
                      <label>Status:</label>
                      <span className={`status-badge ${appModule.versionStatus === 'UpToDate' ? 'status-uptodate' : 'status-update-available'}`}>
                        {appModule.versionStatus === 'UpToDate' ? 'Up-to-date ✓' : 'New version available'}
                      </span>
                    </div>
                  </div>

                  <div className="module-actions">
                    <button 
                      type="button"
                      className="btn-secondary"
                      onClick={() => handleOpenChangeVersion(appModule.moduleId, appModule.version)}
                    >
                      Change Version
                    </button>
                    <button 
                      type="button"
                      className="btn-secondary"
                      onClick={() => handleViewChangelog(appModule)}
                    >
                      View Changelog
                    </button>
                    <button 
                      type="button"
                      className="btn-danger" 
                      onClick={() => handleRemoveModule(appModule.moduleId, appModule.moduleName)}
                    >
                      Remove
                    </button>
                  </div>

                  <p className="text-muted module-integrated-date">
                    Integrated on {new Date(appModule.integratedAt).toLocaleDateString()}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Section 3: Integration Documentation */}
        <div className="app-detail__panel app-detail__docs-panel">
          <h2>Integration Documentation</h2>
          
          <div className="docs-section">
            <h3>📦 Step 1: Install the SDK</h3>
            <p>Add the Primus Identity Validator SDK to your {currentApplication.stack} project:</p>
            <div className="code-block-container">
              <pre className="code-block">
                <code>{getInstallCommand()}</code>
              </pre>
              <button 
                type="button"
                className="copy-btn"
                onClick={() => handleCopy(getInstallCommand(), 'install')}
              >
                {copiedText === 'install' ? '✓ Copied' : '📋 Copy'}
              </button>
            </div>
          </div>

          <div className="docs-section">
            <h3>⚙️ Step 2: Configure Your Application</h3>
            <p>Add your Primus Client ID and Azure AD credentials to your configuration:</p>
            <div className="alert alert-info">
              <strong>Note:</strong> The <code>PrimusClientId</code> is pre-filled with your application's ID. 
              Replace the Azure AD placeholders with YOUR tenant's credentials from your Azure AD App Registration.
            </div>
            <div className="code-block-container">
              <pre className="code-block">
                <code>{getConfigTemplate()}</code>
              </pre>
              <button 
                type="button"
                className="copy-btn"
                onClick={() => handleCopy(getConfigTemplate(), 'config')}
              >
                {copiedText === 'config' ? '✓ Copied' : '📋 Copy'}
              </button>
            </div>
            
            <div className="config-help">
              <h4>📘 Configuration Guide:</h4>
              <ul>
                <li>
                  <strong>&lt;YOUR_TENANT_ID&gt;</strong>: Your Azure AD tenant ID (found in Azure Portal → Azure Active Directory → Overview)
                </li>
                <li>
                  <strong>&lt;YOUR_AZURE_CLIENT_ID&gt;</strong>: Your application's client ID from Azure AD App Registration
                </li>
                <li>
                  <strong>&lt;TENANT_ID&gt;</strong>: Same as YOUR_TENANT_ID (used in the authority URL)
                </li>
                <li>
                  <strong>Audience</strong>: Format is <code>api://&lt;YOUR_AZURE_CLIENT_ID&gt;</code> - this identifies your API
                </li>
              </ul>
              <p className="help-link">
                💡 <a href="https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app" target="_blank" rel="noopener noreferrer">
                  Learn how to find these values in Azure Portal
                </a>
              </p>
            </div>
          </div>

          <div className="docs-section">
            <h3>🌱 Required Environment Variables</h3>
            <ul className="env-list">
              <li><code>PRIMUS_CLIENT_ID</code> = {currentApplication.primusClientId}</li>
              <li><code>AZURE_TENANT_ID</code> = &lt;YOUR_TENANT_ID&gt;</li>
              <li><code>AZURE_CLIENT_ID</code> = &lt;YOUR_AZURE_CLIENT_ID&gt;</li>
              <li><code>AZURE_AUDIENCE</code> = api://&lt;YOUR_AZURE_CLIENT_ID&gt;</li>
            </ul>
            <p className="muted">Tip: store these in your deployment secrets manager and reference them in your config templates above.</p>
          </div>

          <div className="docs-section">
            <h3>🔧 Step 3: Initialize in Your Code</h3>
            <p>Add the Primus Identity Validator to your application startup:</p>
            <div className="code-block-container">
              <pre className="code-block">
                <code>{getCodeSnippet()}</code>
              </pre>
              <button 
                type="button"
                className="copy-btn"
                onClick={() => handleCopy(getCodeSnippet(), 'code')}
              >
                {copiedText === 'code' ? '✓ Copied' : '📋 Copy'}
              </button>
            </div>
          </div>

          <div className="docs-section">
            <h3>🔒 Step 4: Protected Routes Guide</h3>
            <p>Apply authentication to your API endpoints:</p>
            {currentApplication.stack === 'NodeJS' && (
              <div className="code-block-container">
                <pre className="code-block">
                  <code>{`app.get("/api/me", auth.requireAuth, handler);`}</code>
                </pre>
              </div>
            )}
            {currentApplication.stack === 'DotNet' && (
              <div className="code-block-container">
                <pre className="code-block">
                  <code>{`[Authorize]
public class MyController : ControllerBase
{
    // Your protected endpoints here
}`}</code>
                </pre>
              </div>
            )}
          </div>

          <div className="docs-section">
            <h3>📚 Additional Resources</h3>
            <ul className="resources-list">
              <li>
                <a href="https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app" target="_blank" rel="noopener noreferrer">
                  How to register an Azure AD Application
                </a>
              </li>
              <li>
                <a href="#" onClick={(e) => { e.preventDefault(); navigate('/modules'); }}>
                  View all available Primus modules
                </a>
              </li>
              <li>
                <a href="https://github.com/akkikhan/Primus-SaaS" target="_blank" rel="noopener noreferrer">
                  GitHub Repository & Examples
                </a>
              </li>
            </ul>
          </div>

          <div className="docs-actions">
            <button 
              type="button" 
              className="btn-primary"
              onClick={handleCopyAll}
            >
              {copiedText === 'all-docs' ? '✓ All Copied!' : '📋 Copy All'}
            </button>
            <button 
              type="button" 
              className="btn-secondary"
              onClick={handleDownloadPdf}
              title="Download PDF with integration steps"
            >
              {copiedText === 'pdf' ? '✓ PDF Downloaded' : '📄 Export PDF'}
            </button>
          </div>
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
                      {version.version} {version.isBreakingChange ? '⚠️ Breaking' : ''}
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

      {/* Change Version Modal */}
      {showChangeVersion && changingModule && (
        <div className="modal-overlay" onClick={() => setShowChangeVersion(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Change {changingModule.moduleName} Version</h2>
            <p>Current version: <strong>v{changingModule.version}</strong></p>
            <p>Latest available: <strong>v{changingModule.latestVersion}</strong></p>
            
            <div className="form-group">
              <label htmlFor="new-version">Select New Version</label>
              <select
                id="new-version"
                value={newVersion}
                onChange={(e) => setNewVersion(e.target.value)}
              >
                <option value="">Choose a version...</option>
                {modules
                  .find(m => m.id === changingModule.moduleId)
                  ?.moduleVersions?.map((version) => (
                    <option key={version.id} value={version.version}>
                      v{version.version} {version.isBreakingChange ? '⚠️ Breaking Change' : ''}
                      {version.version === changingModule.version ? ' (Current)' : ''}
                    </option>
                  ))}
              </select>
            </div>

            <div className="modal-actions">
              <button type="button" onClick={() => setShowChangeVersion(false)}>Cancel</button>
              <button 
                type="button" 
                className="btn-primary"
                onClick={handleChangeVersion}
                disabled={!newVersion || newVersion === changingModule.version}
              >
                Apply Version Change
              </button>
            </div>
          </div>
        </div>
      )}

      {/* View Changelog Modal */}
      {showChangelog && changelogModule && (
        <div className="modal-overlay" onClick={() => setShowChangelog(false)}>
          <div className="modal modal-large" onClick={(e) => e.stopPropagation()}>
            <h2>{changelogModule.moduleName} - Changelog</h2>
            
            <div className="changelog-content">
              <div className="changelog-item">
                <div className="changelog-header">
                  <h3>Version {changelogModule.version} {changelogModule.versionStatus === 'UpToDate' ? '(Current)' : ''}</h3>
                  {changelogModule.isBreakingChange && (
                    <span className="breaking-badge">⚠️ Breaking Change</span>
                  )}
                </div>
                <div className="changelog-body">
                  <h4>Release Notes</h4>
                  <p>{changelogModule.releaseNotes || 'No release notes available for this version.'}</p>
                  
                  {changelogModule.changelog && (
                    <div className="changelog-section">
                      <h4>Detailed Changelog</h4>
                      <pre className="changelog-text">
                        {changelogModule.changelog}
                      </pre>
                    </div>
                  )}
                  
                  {changelogModule.demoCode && (
                    <div className="changelog-section">
                      <h4>Demo Code</h4>
                      <pre className="demo-code">
                        <code>{changelogModule.demoCode}</code>
                      </pre>
                    </div>
                  )}
                </div>
              </div>

              {changelogModule.latestVersion !== changelogModule.version && (
                <div className="changelog-notice">
                  <p>📢 <strong>A new version (v{changelogModule.latestVersion}) is available!</strong></p>
                  <button 
                    type="button"
                    className="btn-primary"
                    onClick={() => {
                      setShowChangelog(false);
                      handleOpenChangeVersion(changelogModule.moduleId, changelogModule.version);
                    }}
                  >
                    Update to Latest Version
                  </button>
                </div>
              )}
            </div>

            <div className="modal-actions">
              <button type="button" onClick={() => setShowChangelog(false)}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Application Modal */}
      {showEditApp && (
        <div className="modal-overlay" onClick={() => setShowEditApp(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Edit Application</h2>
            <form onSubmit={handleEditSave}>
              <div className="form-group">
                <label htmlFor="editName">Application Name</label>
                <input
                  id="editName"
                  type="text"
                  value={editForm.name}
                  onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="editStack">Technology Stack</label>
                <select
                  id="editStack"
                  value={editForm.stack}
                  disabled
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
                <label htmlFor="editDescription">Description (Optional)</label>
                <textarea
                  id="editDescription"
                  value={editForm.description}
                  onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                  rows={3}
                />
              </div>
              <div className="modal-actions">
                <button type="button" onClick={() => setShowEditApp(false)}>Cancel</button>
                <button type="submit">Save</button>
              </div>
            </form>
          </div>
        </div>
      )}

      <Outlet />
    </section>
  );
};
