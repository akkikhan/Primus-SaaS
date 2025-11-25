import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';

export default function Home() {
  return (
    <Layout
      title="Primus SaaS Platform Documentation"
      description="Production-ready backend modules for Node.js and .NET. Reduce development time with enterprise-grade authentication, logging, and more."
    >
      <main className="hero" style={{ padding: '6rem 0', background: 'linear-gradient(180deg, #0b1520 0%, #0f1e2e 100%)' }}>
        <div className="container">
          <div className="row" style={{ alignItems: 'center' }}>
            <div className="col col--7">
              <h1 className="hero__title" style={{ fontSize: '3.5rem', marginBottom: '1.5rem', fontWeight: 400, color: '#ffffff' }}>
                A platform that empowers developers to build secure, scalable applications
              </h1>
              <p className="hero__subtitle" style={{ fontSize: '1.35rem', lineHeight: '1.8', marginBottom: '2.5rem', color: '#cbd5e1', fontWeight: 300 }}>
                Primus provides production-ready, modular backend components as reusable packages. Integrate enterprise-grade authentication, logging, and more in minutes. Build faster without sacrificing security or scalability.
              </p>
              <div style={{ display: 'flex', gap: '1.25rem', flexWrap: 'wrap', marginBottom: '2rem' }}>
                <Link className="button button--primary button--lg" to="/docs" style={{ padding: '0.875rem 2rem', fontSize: '1.1rem', borderRadius: '6px' }}>
                  Get Started
                </Link>
                <Link className="button button--secondary button--lg" to="/docs/modules/client-integration-guide" style={{ padding: '0.875rem 2rem', fontSize: '1.1rem', borderRadius: '6px' }}>
                  View Documentation
                </Link>
              </div>
            </div>
            <div className="col col--5">
              <div style={{ background: 'rgba(15, 23, 42, 0.6)', padding: '1.75rem', borderRadius: '12px', border: '1px solid rgba(148, 163, 184, 0.1)', backdropFilter: 'blur(10px)' }}>
                <p style={{ marginBottom: '1rem', fontSize: '0.875rem', color: '#94a3b8', textTransform: 'uppercase', letterSpacing: '0.05em', fontWeight: 500 }}>
                  Multi-issuer Configuration
                </p>
                <pre style={{ margin: 0, fontSize: '0.875rem', lineHeight: '1.6', background: 'rgba(30, 41, 59, 0.5)', padding: '1rem', borderRadius: '8px', color: '#e2e8f0', overflowX: 'auto' }}>
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
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Features Section */}
      <section style={{ padding: '5rem 0', background: '#f8fafc' }}>
        <div className="container">
          <h2 style={{ textAlign: 'center', marginBottom: '1rem', fontSize: '2.75rem', fontWeight: 400, color: '#0f172a' }}>
            Features that power your development
          </h2>
          <p style={{ textAlign: 'center', marginBottom: '4rem', fontSize: '1.125rem', color: '#64748b', maxWidth: '700px', margin: '0 auto 4rem' }}>
            Built for modern application development with enterprise-grade security and scalability
          </p>
          <div className="row">
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <div style={{ padding: '2rem', background: '#ffffff', borderRadius: '12px', border: '1px solid #e2e8f0', height: '100%', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1rem', color: '#0f172a', fontWeight: 500 }}>Multi-issuer Authentication</h3>
                <p style={{ color: '#475569', lineHeight: '1.7' }}>Support for Azure AD, LocalAuth, custom JWT, and any OIDC-compliant identity provider with unified middleware configuration.</p>
              </div>
            </div>
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <div style={{ padding: '2rem', background: '#ffffff', borderRadius: '12px', border: '1px solid #e2e8f0', height: '100%', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1rem', color: '#0f172a', fontWeight: 500 }}>Zero Runtime Dependencies</h3>
                <p style={{ color: '#475569', lineHeight: '1.7' }}>All modules run entirely within your application. No external API calls, no PII storage, complete infrastructure control.</p>
              </div>
            </div>
            <div className="col col--4" style={{ marginBottom: '2rem' }}>
              <div style={{ padding: '2rem', background: '#ffffff', borderRadius: '12px', border: '1px solid #e2e8f0', height: '100%', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1rem', color: '#0f172a', fontWeight: 500 }}>Multi-Platform Support</h3>
                <p style={{ color: '#475569', lineHeight: '1.7' }}>Consistent APIs across Node.js (Express, NestJS) and .NET (Minimal API, MVC) with comprehensive TypeScript support.</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Getting Started Section */}
      <section style={{ padding: '5rem 0', background: '#0f172a' }}>
        <div className="container">
          <h2 style={{ textAlign: 'center', marginBottom: '1rem', fontSize: '2.75rem', fontWeight: 400, color: '#ffffff' }}>
            Get started in minutes
          </h2>
          <p style={{ textAlign: 'center', marginBottom: '4rem', fontSize: '1.125rem', color: '#94a3b8', maxWidth: '700px', margin: '0 auto 4rem' }}>
            Install Primus modules with your preferred package manager and integrate into your application
          </p>
          <div className="row">
            <div className="col col--6">
              <div style={{ padding: '2.5rem', background: 'rgba(30, 41, 59, 0.5)', borderRadius: '12px', marginBottom: '1.5rem', border: '1px solid rgba(148, 163, 184, 0.1)' }}>
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1.5rem', color: '#e2e8f0', fontWeight: 500 }}>Node.js / Express</h3>
                <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.25rem', borderRadius: '8px', marginBottom: '1.5rem', border: '1px solid rgba(148, 163, 184, 0.1)' }}>
                  <code style={{ color: '#e2e8f0', fontSize: '0.9rem' }}>{`npm install @primus-saas/identity-validator`}</code>
                </pre>
                <Link to="/docs/modules/client-integration-guide" style={{ color: '#14b8a6', textDecoration: 'none', fontSize: '1rem', fontWeight: 500 }}>
                  View Integration Guide →
                </Link>
              </div>
            </div>
            <div className="col col--6">
              <div style={{ padding: '2.5rem', background: 'rgba(30, 41, 59, 0.5)', borderRadius: '12px', marginBottom: '1.5rem', border: '1px solid rgba(148, 163, 184, 0.1)' }}>
                <h3 style={{ fontSize: '1.5rem', marginBottom: '1.5rem', color: '#e2e8f0', fontWeight: 500 }}>.NET / Minimal API</h3>
                <pre style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '1.25rem', borderRadius: '8px', marginBottom: '1.5rem', border: '1px solid rgba(148, 163, 184, 0.1)' }}>
                  <code style={{ color: '#e2e8f0', fontSize: '0.9rem' }}>{`dotnet add package PrimusSaaS.Identity.Validator`}</code>
                </pre>
                <Link to="/docs/modules/client-integration-guide" style={{ color: '#14b8a6', textDecoration: 'none', fontSize: '1rem', fontWeight: 500 }}>
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
