// @ts-check

const config = {
  title: 'Primus Integration Docs',
  tagline: 'Public integration recipes for Node.js and .NET 8',
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
  ]
};

module.exports = config;
