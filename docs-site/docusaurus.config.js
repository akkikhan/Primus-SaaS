// @ts-check

const config = {
  title: 'Primus SaaS Platform Documentation',
  tagline: 'Production-ready backend modules for Node.js and .NET - Reduce development time with enterprise-grade authentication, logging, and more',
  url: 'https://akkikhan.github.io',
  baseUrl: '/Primus-SaaS/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  trailingSlash: false,
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn'
    }
  },
  favicon: 'img/favicon.ico',
  organizationName: 'akkikhan',
  projectName: 'Primus-SaaS',
  deploymentBranch: 'gh-pages',
  i18n: {
    defaultLocale: 'en',
    locales: ['en']
  },
  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: undefined
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css')
        }
      }
    ]
  ],
  plugins: [
    [
      '@docusaurus/plugin-client-redirects',
      {
      redirects: [
            // Legacy identity pages
            { from: '/docs/modules/identity-validator-nodejs', to: '/docs/modules/identity-quick-start' },
            { from: '/docs/modules/identity-validator-dotnet', to: '/docs/modules/identity-quick-start' },
            { from: '/docs/modules/identity-configuration', to: '/docs/intro' },
            { from: '/docs/modules/identity-token-generation', to: '/docs/intro' },
            { from: '/docs/modules/identity-tenant-resolver', to: '/docs/intro' },
            { from: '/docs/modules/identity-error-reference', to: '/docs/intro' },
            // Legacy logging pages
            { from: '/docs/modules/logging-nodejs', to: '/docs/intro' },
            { from: '/docs/modules/logging-dotnet', to: '/docs/intro' },
            { from: '/docs/modules/logging-configuration', to: '/docs/intro' },
            { from: '/docs/modules/logging-middleware', to: '/docs/intro' },
            { from: '/docs/modules/logging-enterprise-features', to: '/docs/intro' },
            { from: '/docs/modules/logging-targets', to: '/docs/intro' },
            // Non-docs-prefixed paths
            { from: '/modules/client-integration-guide', to: '/docs/intro' },
            { from: '/modules/identity-validator', to: '/docs/modules/identity-quick-start' },
            { from: '/modules/logging-module', to: '/docs/modules/logging-module' },
            // Redirect /docs root to intro
            { from: '/docs', to: '/docs/intro' }
        ]
      }
    ]
  ],
  themeConfig: {
    navbar: {
      title: 'Primus SaaS Platform',
      logo: {
        alt: 'Primus Logo',
        src: 'img/logo.png',
        href: '/docs/intro',
        width: 120,
        height: 40
      },
      items: [
        {
          type: 'doc',
          docId: 'intro',
          position: 'left',
          label: 'Documentation'
        },
        {
          href: 'https://github.com/akkikhan/Primus-SaaS',
          label: 'GitHub',
          position: 'right'
        }
      ]
    },
    footer: {
      style: 'dark',
      logo: {
        alt: 'Primus Logo',
        src: 'img/logo.png',
        width: 150
      },
      copyright: `Copyright © ${new Date().getFullYear()} Primus SaaS Platform. Built with Docusaurus.`
    }
  }
};

module.exports = config;
