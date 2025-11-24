import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';

export default function Home() {
  return (
    <Layout
      title="Primus Integration Docs"
      description="Multi-issuer auth recipes for Node.js and .NET 8"
    >
      <main className="hero hero--dark" style={{ padding: '5rem 0' }}>
        <div className="container">
          <div className="row">
            <div className="col col--6">
              <h1 className="hero__title">Integrate Primus fast.</h1>
              <p className="hero__subtitle">
                Copy-paste guides for Azure AD + LocalAuth across Node.js and .NET 8. Ready for
                customers, ready for handoff.
              </p>
              <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                <Link className="button button--primary button--lg" to="/docs">
                  View Docs
                </Link>
                <Link className="button button--secondary button--lg" to="/docs/modules/identity-validator-nodejs">
                  Quick Start Guide
                </Link>
              </div>
            </div>
            <div className="col col--6">
              <div className="codeFrame">
                <p style={{ marginBottom: '0.5rem', opacity: 0.8 }}>Multi-issuer config</p>
                <pre style={{ margin: 0 }}>
{`issuers: [
  { name: 'AzureAD', type: 'oidc', issuer: '.../v2.0' },
  { name: 'LocalAuth', type: 'jwt', issuer: 'http://localhost:4000' }
]`}
                </pre>
              </div>
            </div>
          </div>
        </div>
      </main>
    </Layout>
  );
}
