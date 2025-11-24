// @ts-check

const config = {
  title: 'Primus SaaS Platform Documentation',
  tagline: 'Production-ready backend modules for Node.js and .NET - Reduce development time with enterprise-grade authentication, logging, and more',
  url: 'https://akkikhan.github.io',
  baseUrl: '/Primus-SaaS/',
  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn'
    }
  },
  favicon: 'img/favicon.ico',
  organizationName: 'akkikhan',
  projectName: 'Primus-SaaS',
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
  themeConfig: {
    navbar: {
      title: 'Primus SaaS Platform',
      logo: {
        alt: 'Primus Logo',
        src: 'img/logo.png',
        href: '/docs',
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
