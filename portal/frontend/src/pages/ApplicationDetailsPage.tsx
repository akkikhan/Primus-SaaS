import { useParams, Link, Outlet, useNavigate } from 'react-router-dom';
import { useApplicationsStore, type IntegratedModule } from '../state/applicationsStore';
import { useModulesStore } from '../state/modulesStore';
import { useEffect, useMemo, useState } from 'react';
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
  const [changelogModule, setChangelogModule] = useState<IntegratedModule | null>(null);
  const [showEditApp, setShowEditApp] = useState(false);
  const [editForm, setEditForm] = useState({ name: '', stack: '', description: '' });
  const [activeTab, setActiveTab] = useState<'overview' | 'integration'>('overview');
  const [activeModuleTab, setActiveModuleTab] = useState<number | null>(null);
  const docsBaseUrl = (import.meta.env.VITE_DOCS_BASE_URL as string | undefined)?.replace(/\/$/, '') || 'https://akkikhan.github.io/Primus-SaaS';
  const docsIntegrationUrl = `${docsBaseUrl}/docs/modules/client-integration-guide`;
  const githubRepoUrl = 'https://github.com/akkikhan/Primus-SaaS';
  const stackInfo: Record<string, { label: string; tone: string }> = {
    DotNet: { label: '.NET', tone: 'tone-dotnet' },
    NodeJS: { label: 'Node.js', tone: 'tone-node' },
    'NodeJS-Nest': { label: 'NestJS', tone: 'tone-node' },
    TypeScriptLib: { label: 'TypeScript Library', tone: 'tone-ts' },
    Python: { label: 'Python', tone: 'tone-python' },
    'Python-FastAPI': { label: 'FastAPI', tone: 'tone-python' }
  };

  useEffect(() => {
    if (id) {
      void fetchApplication(Number(id));
      void fetchModules();
    }
  }, [id, fetchApplication, fetchModules]);

  // Set active module tab to first module when switching to integration tab
  useEffect(() => {
    if (activeTab === 'integration' && currentApplication?.integratedModules && currentApplication.integratedModules.length > 0 && !activeModuleTab) {
      setActiveModuleTab(currentApplication.integratedModules[0].moduleId);
    }
  }, [activeTab, currentApplication, activeModuleTab]);

  if (isLoading || !currentApplication) {
    return <p>Loading application...</p>;
  }

  const handleAddModule = async () => {
    if (selectedModuleId && selectedVersionId && id) {
      try {
        await addModule(Number(id), selectedModuleId, selectedVersionId);
        setShowAddModule(false);
        setSelectedModuleId(null);
        setSelectedVersionId(null);
      } catch {
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
      } catch {
        // Error handled by store
      }
    }
  };

  const handleViewChangelog = (module: IntegratedModule) => {
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
API_AUDIENCE=${currentApplication.primusClientId}
AZURE_AD_ISSUER=https://login.microsoftonline.com/<TENANT_ID>/v2.0
AZURE_AD_AUTHORITY=https://login.microsoftonline.com/<TENANT_ID>/v2.0
LOCAL_ISSUER=http://localhost:4000
LOCAL_SECRET=<LOCAL_DEV_SECRET>

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

  // Filter out already assigned modules
  const availableModules = modules.filter(m =>
    !currentApplication?.integratedModules?.some(im => im.moduleId === m.id)
  );
  const sortedModuleVersions = useMemo(() => {
    if (!selectedModule) return [];
    const versions = [...(selectedModule.moduleVersions ?? [])];
    return versions.sort((a, b) => new Date(b.releasedAt || '').getTime() - new Date(a.releasedAt || '').getTime());
  }, [selectedModule]);

  const latestVersionLabel = (moduleId: number) => {
    const module = modules.find(m => m.id === moduleId);
    const latest = module?.latestVersion || module?.moduleVersions?.[0]?.version;
    return latest ? ` • Latest v${latest}` : '';
  };

  const getModuleDocLink = (moduleName: string) => {
    const normalized = moduleName.toLowerCase().includes('log') ? '#add-logging' : '#5-integration-steps';
    return `${docsIntegrationUrl}${normalized.startsWith('#') ? normalized : `#${normalized}`}`;
  };

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

  // Generate integration documentation based on stack and module
  const getInstallCommand = (moduleName?: string) => {
    const module = moduleName || 'Identity Validator';
    const stack = currentApplication.stack;
    
    if (module.toLowerCase() === 'identity validator') {
      switch (stack) {
        case 'DotNet':
          return 'dotnet add package PrimusSaaS.Identity.Validator';
        case 'NodeJS':
        case 'NodeJS-Nest':
        case 'TypeScriptLib':
          return 'npm install @primus-saas/identity-validator';
        case 'Python':
        case 'Python-FastAPI':
          return 'pip install primus-identity-validator';
        default:
          return '';
      }
    } else if (module.toLowerCase() === 'logging' || module.toLowerCase() === 'logging sdk') {
      switch (stack) {
        case 'DotNet':
          return 'dotnet add package PrimusSaaS.Logging';
        case 'NodeJS':
        case 'NodeJS-Nest':
        case 'TypeScriptLib':
          return 'npm install @primus-saas/logging';
        case 'Python':
        case 'Python-FastAPI':
          return 'pip install primus-logging';
        default:
          return '';
      }
    }
    return '';
  };

  const getConfigTemplate = (moduleName?: string) => {
    const primusClientId = currentApplication.primusClientId;
    const module = moduleName || 'Identity Validator';
    const stack = currentApplication.stack;

    if (module.toLowerCase() === 'identity validator') {
      switch (stack) {
        case 'DotNet':
          return `{
  "Primus": {
    "AzureAd": {
      "TenantId": "<YOUR_TENANT_ID>"
    }
  }
}`;
        case 'NodeJS':
        case 'NodeJS-Nest':
        case 'TypeScriptLib':
          return `// No configuration file needed.
// Pass issuers directly to middleware/validator:
// See code snippet below for inline configuration.`;
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
    } else if (module.toLowerCase() === 'logging' || module.toLowerCase() === 'logging sdk') {
      switch (stack) {
        case 'DotNet':
          return `{
  "Logging": {
    "Primus": {
      "Targets": [
        {
          "Type": "console",
          "Format": "PrettyPrint"
        },
        {
          "Type": "file",
          "Path": "logs/app.log"
        }
      ]
    }
  }
}`;
        case 'NodeJS':
        case 'NodeJS-Nest':
        case 'TypeScriptLib':
          return `// No configuration file needed.
// Configure logging in code:
// See code snippet below for setup.`;
        default:
          return '';
      }
    }
    return '';
  };

  const getCodeSnippet = (moduleName?: string) => {
    const primusClientId = currentApplication.primusClientId;
    const module = moduleName || 'Identity Validator';
    const stack = currentApplication.stack;

    if (module.toLowerCase() === 'identity validator') {
      switch (stack) {
        case 'DotNet':
          return `// Program.cs
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
            Authority = "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
            Audiences = new List<string> { "${primusClientId}" }
        }
    };
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();`;
      case 'NodeJS':
        return `// server.ts
import express from "express";
import { primusIdentityMiddleware } from "primus-identity-validator";

const app = express();

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: "AzureAD",
      type: "oidc",
      issuer: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      authority: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      audiences: ["${primusClientId}"]
    }
  ]
});

app.get("/api/me", primusAuth, (req, res) => {
  res.json({ user: req.primusUser });
});`;
      case 'NodeJS-Nest':
        return `// auth.module.ts
import { Module, MiddlewareConsumer, NestModule } from "@nestjs/common";
import { primusIdentityMiddleware } from "primus-identity-validator";

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: "AzureAD",
      type: "oidc",
      issuer: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      authority: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      audiences: ["${primusClientId}"]
    }
  ]
});

@Module({
  providers: [],
  exports: []
})
export class AuthModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    consumer.apply(primusAuth).forRoutes("*");
  }
}`;
      case 'TypeScriptLib':
        return `// validator.ts
import { PrimusIdentityValidator } from "primus-identity-validator";

export const validator = new PrimusIdentityValidator({
  issuers: [
    {
      name: "AzureAD",
      type: "oidc",
      issuer: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      authority: "https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0",
      audiences: ["${primusClientId}"]
    }
  ]
});

const token = "eyJ...";  // Without "Bearer " prefix
const result = await validator.validateToken(token);

if (result.isValid) {
  console.log("Claims:", result.claims);
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
from primus_identity_validator import PrimusIdentityValidator

app = FastAPI()
validator = PrimusIdentityValidator(
    primus_client_id="${primusClientId}",
    mode="AzureAd",
    azure_ad={
        "tenant_id": "<YOUR_TENANT_ID>",
        "client_id": "<YOUR_AZURE_CLIENT_ID>",
        "audience": "api://<YOUR_AZURE_CLIENT_ID>"
    }
)

@app.get("/api/me")
async def get_user(user=Depends(validator.require_auth)):
    return {"user": user}`;
      default:
        return '';
    }
  } else if (module.toLowerCase() === 'logging' || module.toLowerCase() === 'logging sdk') {
    switch (stack) {
      case 'DotNet':
        return `// Program.cs
using PrimusSaaS.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPrimusLogging(options =>
{
    options.Targets = new List<LogTarget>
    {
        new ConsoleTarget { Format = "PrettyPrint" },
        new FileTarget { Path = "logs/app.log" }
    };
});

var app = builder.Build();
app.UsePrimusLogging();  // Adds HTTP context enrichment
app.Run();`;
      case 'NodeJS':
      case 'NodeJS-Nest':
        return `// logger.ts
import { createPrimusLogger } from "@primus-saas/logging";

export const logger = createPrimusLogger({
  targets: [
    { type: "console", format: "pretty" },
    { type: "file", path: "logs/app.log" }
  ]
});

// Usage
logger.info("User logged in", { userId: "123" });
logger.error("Failed to process", { error: err });`;
      case 'TypeScriptLib':
        return `// logger.ts
import { PrimusLogger } from "@primus-saas/logging";

export const logger = new PrimusLogger({
  targets: [
    { type: "console", format: "json" },
    { type: "file", path: "logs/lib.log" }
  ]
});

export function logOperation(operation: string, data: any) {
  logger.info(\`Operation: \${operation}\`, data);
}`;
      default:
        return '';
    }
  }
  return '';
};

  const getLoginScript = () => `
import { PublicClientApplication } from '@azure/msal-browser';

const msalConfig = {
  auth: {
    clientId: '<AZURE_AD_CLIENT_ID>',
    authority: 'https://login.microsoftonline.com/<TENANT_ID>',
    redirectUri: window.location.origin + '/login'
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false
  }
};

export const msalInstance = new PublicClientApplication(msalConfig);

export async function signInWithAzure() {
  const response = await msalInstance.loginPopup({
    scopes: ['openid', 'profile', 'email'],
    prompt: 'select_account'
  });

  if (!response.idToken) {
    throw new Error('Azure AD did not return an ID token.');
  }

  // Send the ID token to your backend to create a Primus session
  await fetch('/api/auth/azure', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ idToken: response.idToken })
  });
}
`.trim();

  return (
    <section className="app-detail">
      {/* Breadcrumb Navigation */}
      <nav className="breadcrumb">
        <Link to="/applications">Applications</Link>
        <span className="separator">&gt;</span>
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
                {copiedText === 'header-clientId' ? 'Copied' : 'Copy'}
              </button>
            </div>
          </div>
          <div className="header-field">
            <label>Technology Stack:</label>
            <span className={`stack-chip ${stackInfo[currentApplication.stack]?.tone ?? 'tone-neutral'}`}>
              <span className="stack-dot" />
              {stackInfo[currentApplication.stack]?.label ?? currentApplication.stack}
            </span>
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

      {/* Tabs Navigation */}
      <div className="app-tabs">
        <button
          className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button
          className={`tab-btn ${activeTab === 'integration' ? 'active' : ''}`}
          onClick={() => setActiveTab('integration')}
        >
          Integration Guide
        </button>
      </div>

      <div className="app-detail__content">
        {activeTab === 'overview' ? (
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
                    <span className={`stack-chip ${stackInfo[currentApplication.stack]?.tone ?? 'tone-neutral'}`}>
                      <span className="stack-dot" />
                      {stackInfo[currentApplication.stack]?.label ?? currentApplication.stack}
                    </span>
                  </div>
                </div>
                <div className="info-item">
                  <label>Primus App ID</label>
                  <div className="info-value copy-container">
                    <code className="code-inline">{currentApplication.primusClientId}</code>
                    <button
                      type="button"
                      className="copy-btn"
                      onClick={() => handleCopy(currentApplication.primusClientId, 'primusClientId')}
                      title="Copy to clipboard"
                    >
                      {copiedText === 'primusClientId' ? 'Copied' : 'Copy'}
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
                            {appModule.versionStatus === 'UpToDate' ? 'Up to date' : 'New version available'}
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
          </div>
        ) : (
          /* Integration Tab Content */
          <div className="app-detail__panel app-detail__docs-panel">
            <div className="docs-hero">
              <div className="docs-hero__text">
                <p className="eyebrow">Integration playbook</p>
                <h2>Integration Documentation</h2>
                <p className="docs-subtitle">
                  Ship-ready steps tuned for {stackInfo[currentApplication.stack]?.label ?? currentApplication.stack} teams.
                </p>
                <div className="docs-links">
                  <a href={docsIntegrationUrl} target="_blank" rel="noopener noreferrer" className="docs-link-primary">
                    View Full Documentation ↗
                  </a>
                  <a href={githubRepoUrl} target="_blank" rel="noopener noreferrer" className="docs-link-secondary">
                    GitHub Repository ↗
                  </a>
                </div>
              </div>
              <div className="docs-hero__actions">
                <button
                  type="button"
                  className="btn-ghost"
                  onClick={handleCopyAll}
                >
                  {copiedText === 'all-docs' ? 'All content copied' : 'Copy all'}
                </button>
                <button
                  type="button"
                  className="btn-ghost"
                  onClick={handleDownloadPdf}
                  title="Download PDF with integration steps"
                >
                  {copiedText === 'pdf' ? 'PDF downloaded' : 'Export PDF'}
                </button>
              </div>
            </div>

            <div className="docs-inline-grid">
              <div className="doc-quick-card">
                <div className="doc-quick-card__header">
                  <div>
                    <p className="eyebrow">Login</p>
                    <h3>Azure AD login app script</h3>
                  </div>
                  <button
                    type="button"
                    className="copy-btn copy-btn--solid"
                    onClick={() => handleCopy(getLoginScript(), 'login-script')}
                  >
                    {copiedText === 'login-script' ? 'Copied' : 'Copy script'}
                  </button>
                </div>
                <p className="docs-subtitle">
                  MSAL starter to sign in with Azure AD and hand the ID token to the Primus portal backend.
                </p>
                <div className="code-block-shell code-block-shell--compact">
                  <div className="code-block-toolbar">
                    <span className="pill">app-login.ts</span>
                    <span className="pill pill-muted">MSAL</span>
                  </div>
                  <pre className="code-block">
                    <code>{getLoginScript()}</code>
                  </pre>
                </div>
              </div>
              <div className="doc-quick-card doc-quick-card--muted">
                <div className="doc-quick-card__header">
                  <div>
                    <p className="eyebrow">Docs source</p>
                    <h3>Synced with Docusaurus</h3>
                  </div>
                </div>
                <p className="docs-subtitle">
                  The portal integration guide mirrors the public Docusaurus docs. Open the live page to cross-check identity and logging steps.
                </p>
                <div className="docs-links">
                  <a href={docsIntegrationUrl} target="_blank" rel="noopener noreferrer" className="docs-link-primary">
                    Open Client Integration Guide ↗
                  </a>
                  <a href={githubRepoUrl} target="_blank" rel="noopener noreferrer" className="docs-link-secondary">
                    View Docs on GitHub ↗
                  </a>
                </div>
              </div>
            </div>

            {/* Module-specific tabs */}
            {currentApplication.integratedModules && currentApplication.integratedModules.length > 0 && (
              <div className="module-tabs">
                {currentApplication.integratedModules.map(module => (
                  <button
                    key={module.moduleId}
                    className={`module-tab-btn ${activeModuleTab === module.moduleId ? 'active' : ''}`}
                    onClick={() => setActiveModuleTab(module.moduleId)}
                  >
                    {module.moduleName}
                  </button>
                ))}
              </div>
            )}

            {currentApplication.integratedModules && currentApplication.integratedModules.length > 0 ? (
              currentApplication.integratedModules.map(module => (
                activeModuleTab === module.moduleId && (
                  <div key={module.moduleId} className="stepper-container">
                    {/* Step 1 */}
                    <div className="step-item">
                      <div className="step-number">1</div>
                      <div className="step-content">
                        <h3>Install the SDK</h3>
                        <p>Add the {module.moduleName} SDK to your {currentApplication.stack} project.</p>
                        <div className="code-block-shell">
                          <div className="code-block-toolbar">
                            <span className="pill">CLI</span>
                            <button
                              type="button"
                              className="copy-btn copy-btn--solid"
                              onClick={() => handleCopy(getInstallCommand(module.moduleName), `install-${module.moduleId}`)}
                            >
                              {copiedText === `install-${module.moduleId}` ? 'Copied' : 'Copy'}
                            </button>
                          </div>
                          <pre className="code-block">
                            <code>{getInstallCommand(module.moduleName)}</code>
                          </pre>
                        </div>
                      </div>
                    </div>

                    {/* Step 2 */}
                    <div className="step-item">
                      <div className="step-number">2</div>
                      <div className="step-content">
                        <h3>Configure your application</h3>
                        <p>Wire your configuration for {module.moduleName}.</p>

                        <div className="inline-callout">
                          <strong>Note:</strong> The <code>PrimusClientId</code> is pre-filled with your application's ID. Replace the
                          placeholders with your credentials.
                        </div>

                        <div className="code-block-shell">
                          <div className="code-block-toolbar">
                            <span className="pill">Config template</span>
                            <button
                              type="button"
                              className="copy-btn copy-btn--solid"
                              onClick={() => handleCopy(getConfigTemplate(module.moduleName), `config-${module.moduleId}`)}
                            >
                              {copiedText === `config-${module.moduleId}` ? 'Copied' : 'Copy'}
                            </button>
                          </div>
                          <pre className="code-block">
                            <code>{getConfigTemplate(module.moduleName)}</code>
                          </pre>
                        </div>
                      </div>
                    </div>

                    {/* Step 3 */}
                    <div className="step-item">
                      <div className="step-number">3</div>
                      <div className="step-content">
                        <h3>Initialize in your code</h3>
                        <p>Add the {module.moduleName} to your application startup.</p>
                        <div className="code-block-shell">
                          <div className="code-block-toolbar">
                            <span className="pill">Starter snippet</span>
                            <button
                              type="button"
                              className="copy-btn copy-btn--solid"
                              onClick={() => handleCopy(getCodeSnippet(module.moduleName), `code-${module.moduleId}`)}
                            >
                              {copiedText === `code-${module.moduleId}` ? 'Copied' : 'Copy'}
                            </button>
                          </div>
                          <pre className="code-block">
                            <code>{getCodeSnippet(module.moduleName)}</code>
                          </pre>
                        </div>
                      </div>
                    </div>

                    {/* Step 4 - Documentation Link */}
                    <div className="step-item">
                      <div className="step-number">4</div>
                      <div className="step-content">
                        <h3>Complete Integration Guide</h3>
                        <p>View the full documentation for detailed setup instructions, examples, and best practices.</p>
                        <a
                          href={getModuleDocLink(module.moduleName)}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="docs-link-primary"
                        >
                          View {module.moduleName} Documentation ↗
                        </a>
                      </div>
                    </div>
                  </div>
                )
              ))
            ) : (
              <div className="empty-state" style={{ marginTop: '2rem' }}>
                <p>No modules integrated yet. Add a module to see integration documentation.</p>
                <button type="button" onClick={() => { setActiveTab('overview'); setShowAddModule(true); }}>
                  + Integrate Your First Module
                </button>
              </div>
            )}
          </div>
        )}
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
                {availableModules.map(module => (
                  <option key={module.id} value={module.id}>
                    {module.name}{latestVersionLabel(module.id)}
                  </option>
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
                  {sortedModuleVersions.map(version => (
                    <option key={version.id} value={version.id}>
                      v{version.version} {version.isBreakingChange ? '(Breaking change)' : ''}
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
                      v{version.version} {version.isBreakingChange ? '(Breaking change)' : ''}
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
                    <span className="breaking-badge">Breaking change</span>
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
                  <p><strong>A newer version (v{changelogModule.latestVersion}) is available.</strong></p>
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
                  <option value="TypeScriptLib" disabled>TypeScript Library (Coming Soon)</option>
                  <option value="Python" disabled>Python FastAPI (Coming Soon)</option>
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
