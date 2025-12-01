import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';

const Stat = ({ title, body }) => (
  <div style={{ padding: '1rem 1.25rem', background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.08)', borderRadius: '12px', color: '#e2e8f0' }}>
    <div style={{ fontWeight: 600, marginBottom: '0.25rem' }}>{title}</div>
    <div style={{ fontSize: '0.95rem', lineHeight: 1.6 }}>{body}</div>
  </div>
);

const FeatureCard = ({ title, body }) => (
  <div style={{ padding: '2rem', background: '#ffffff', borderRadius: '14px', border: '1px solid #e2e8f0', boxShadow: '0 12px 30px rgba(15,23,42,0.06)', height: '100%' }}>
    <h3 style={{ fontSize: '1.3rem', marginBottom: '0.6rem', color: '#0f172a', fontWeight: 650 }}>{title}</h3>
    <p style={{ color: '#475569', lineHeight: 1.7 }}>{body}</p>
  </div>
);

export default function Home() {
  return (
    <Layout
      title="Primus SaaS Platform Documentation"
      description="Production-ready backend modules for Node.js and .NET. Reduce development time with enterprise-grade authentication, logging, and more."
    >
      <main
        className="hero"
        style={{
          padding: '6.5rem 0 5rem',
          background: 'radial-gradient(circle at 20% 20%, rgba(37,99,235,0.08), transparent 35%), radial-gradient(circle at 80% 10%, rgba(20,184,166,0.12), transparent 30%), linear-gradient(180deg, #0b1520 0%, #0f1e2e 100%)'
        }}
      >
        <div className="container">
          <div className="row" style={{ alignItems: 'center', rowGap: '2rem' }}>
            <div className="col col--6">
              <h1 className="hero__title" style={{ fontSize: '3.6rem', marginBottom: '1.25rem', fontWeight: 600, color: '#ffffff', letterSpacing: '-0.02em', lineHeight: 1.15 }}>
                Build secure, observable apps in minutes
              </h1>
              <p className="hero__subtitle" style={{ fontSize: '1.15rem', lineHeight: '1.9', marginBottom: '2rem', color: '#cbd5e1', fontWeight: 300 }}>
                Primus ships two production-ready modules—Identity Validator and Logging—for Node.js and .NET. Keep tokens and logs fully within your stack: zero hosted dependencies, zero PII at Primus.
              </p>
              <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.75rem' }}>
                <Link className="button button--primary button--lg" to="/docs/modules/client-integration-guide">
                  View Client Integration Guide
                </Link>
                <Link className="button button--secondary button--lg" to="/docs/intro">
                  Platform Overview
                </Link>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '0.85rem', maxWidth: '700px' }}>
                <Stat title="No hosted runtime" body="Validation & logging stay in your code. No data leaves your app." />
                <Stat title="Multi-issuer identity" body="Azure AD (JWKS) + local JWT in one app with RBAC helpers." />
                <Stat title="Safe, enriched logs" body="Correlation IDs, scopes, timers, PII masking, Serilog/NLog bridges." />
              </div>
            </div>
            <div className="col col--6" style={{ display: 'flex', justifyContent: 'center' }}>
              <div
                style={{
                  background: '#0f172a',
                  borderRadius: '18px',
                  padding: '1.75rem',
                  border: '1px solid rgba(255,255,255,0.08)',
                  width: '100%',
                  maxWidth: '500px',
                  boxShadow: '0 16px 40px rgba(0,0,0,0.35)'
                }}
              >
                <div style={{ color: '#cbd5e1', fontSize: '0.95rem', marginBottom: '0.75rem', letterSpacing: '0.02em', textTransform: 'uppercase' }}>
                  Multi-issuer configuration
                </div>
                <pre
                  style={{
                    background: '#0b1220',
                    color: '#e2e8f0',
                    padding: '1.25rem',
                    borderRadius: '12px',
                    fontSize: '0.95rem',
                    lineHeight: 1.6,
                    border: '1px solid rgba(255,255,255,0.06)',
                    marginBottom: '1.5rem'
                  }}
                >
                  {`issuers: [
{
  name: 'AzureAD',
  type: 'oidc',
  issuer: 'https://login.microsoft...v2.0',
  audiences: ['api://your-app-id']
},
{
  name: 'LocalAuth',
  type: 'jwt',
  issuer: 'https://auth.company.com',
  secret: process.env.JWT_SECRET
}
]`}
                </pre>
                <div style={{ color: '#cbd5e1', fontSize: '0.95rem', lineHeight: 1.7 }}>
                  Logging targets configured alongside identity:
                  <div style={{ marginTop: '0.35rem', fontFamily: 'monospace', background: '#0b1220', padding: '0.85rem', borderRadius: '10px', border: '1px solid rgba(255,255,255,0.06)' }}>
                    {`targets: [
  { type: 'console', pretty: true },
  { type: 'file', path: 'logs/app.log', async: true },
  { type: 'serilog' }
]`}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Value Section */}
      <section style={{ padding: '4.75rem 0', background: '#ffffff' }}>
        <div className="container">
          <h2
            style={{
              textAlign: 'center',
              marginBottom: '0.85rem',
              fontSize: '2.6rem',
              fontWeight: 600,
              color: '#0f172a',
              letterSpacing: '-0.01em'
            }}
          >
            Why teams choose Primus
          </h2>
          <p
            style={{
              textAlign: 'center',
              marginBottom: '3.1rem',
              fontSize: '1.05rem',
              color: '#475569',
              maxWidth: '880px',
              marginLeft: 'auto',
              marginRight: 'auto',
              lineHeight: 1.8
            }}
          >
            Ship faster without surrendering security or observability. Everything runs in your stack—no external dependencies, no data sharing, no surprise outages.
          </p>
          <div className="row">
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <FeatureCard title="No Hosted Runtime" body="All validation and logging stay in your codebase. Zero traffic to Primus, zero PII held by us." />
            </div>
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <FeatureCard title="Faster Integrations" body="Drop-in middleware, env templates, and ready examples for Express/NestJS and ASP.NET Core." />
            </div>
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <FeatureCard title="Operational Clarity" body="Structured logs, correlation IDs, and diagnostics endpoints so incidents are faster to triage." />
            </div>
          </div>
        </div>
      </section>

      {/* Getting Started Section */}
      <section style={{ padding: '5rem 0', background: '#0f172a' }}>
        <div className="container">
          <h2 style={{ textAlign: 'center', marginBottom: '1.15rem', fontSize: '2.8rem', fontWeight: 500, color: '#ffffff' }}>
            Get started in minutes
          </h2>
          <p
            style={{
              textAlign: 'center',
              marginBottom: '4rem',
              fontSize: '1.125rem',
              color: '#94a3b8',
              maxWidth: '720px',
              margin: '0 auto 4rem',
              lineHeight: 1.8
            }}
          >
            Install Primus modules with your preferred package manager and integrate into your application.
          </p>
          <div className="row" style={{ rowGap: '1.5rem', alignItems: 'stretch' }}>
            <div className="col col--6">
              <div
                style={{
                  padding: '2.25rem',
                  background: 'rgba(30, 41, 59, 0.55)',
                  borderRadius: '12px',
                  border: '1px solid rgba(148, 163, 184, 0.12)',
                  height: '100%'
                }}
              >
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1rem', color: '#e2e8f0', fontWeight: 600 }}>Identity Validator</h3>
                <div style={{ display: 'grid', gap: '0.9rem' }}>
                  <div>
                    <div style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '0.35rem' }}>npm</div>
                    <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.05rem', borderRadius: '10px', margin: 0, border: '1px solid rgba(148,163,184,0.1)' }}>
                      <code style={{ color: '#e2e8f0', fontSize: '0.95rem' }}>{`npm install @primus-saas/identity-validator`}</code>
                    </pre>
                  </div>
                  <div>
                    <div style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '0.35rem' }}>dotnet</div>
                    <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.05rem', borderRadius: '10px', margin: 0, border: '1px solid rgba(148,163,184,0.1)' }}>
                      <code style={{ color: '#e2e8f0', fontSize: '0.95rem' }}>{`dotnet add package PrimusSaaS.Identity.Validator --version 1.3.0`}</code>
                    </pre>
                  </div>
                </div>
                <Link to="/docs/modules/client-integration-guide" style={{ color: '#14b8a6', textDecoration: 'none', fontSize: '1rem', fontWeight: 500, display: 'inline-block', marginTop: '1rem' }}>
                  View Integration Guide →
                </Link>
              </div>
            </div>
            <div className="col col--6">
              <div
                style={{
                  padding: '2.25rem',
                  background: 'rgba(30, 41, 59, 0.55)',
                  borderRadius: '12px',
                  border: '1px solid rgba(148, 163, 184, 0.12)',
                  height: '100%'
                }}
              >
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1rem', color: '#e2e8f0', fontWeight: 600 }}>Logging Module</h3>
                <div style={{ display: 'grid', gap: '0.9rem' }}>
                  <div>
                    <div style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '0.35rem' }}>npm</div>
                    <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.05rem', borderRadius: '10px', margin: 0, border: '1px solid rgba(148,163,184,0.1)' }}>
                      <code style={{ color: '#e2e8f0', fontSize: '0.95rem' }}>{`npm install @primus-saas/logging`}</code>
                    </pre>
                  </div>
                  <div>
                    <div style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '0.35rem' }}>dotnet</div>
                    <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.05rem', borderRadius: '10px', margin: 0, border: '1px solid rgba(148,163,184,0.1)' }}>
                      <code style={{ color: '#e2e8f0', fontSize: '0.95rem' }}>{`dotnet add package PrimusSaaS.Logging --version 1.2.4`}</code>
                    </pre>
                  </div>
                </div>
                <Link to="/docs/modules/client-integration-guide" style={{ color: '#14b8a6', textDecoration: 'none', fontSize: '1rem', fontWeight: 500, display: 'inline-block', marginTop: '1rem' }}>
                  View Integration Guide →
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>
    </Layout>
  );
}
