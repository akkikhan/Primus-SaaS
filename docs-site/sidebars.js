/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    {
      type: 'category',
      label: 'Modules',
      collapsible: true,
      items: [
        'modules/identity-validator',
        'modules/logging-module',
        'modules/notifications',
        'modules/document-renderer',
        'modules/feature-flags',
        'modules/client-integration-guide',
        'modules/live-demo-api',
        'modules/version-matrix'
      ]
    },
    'release-notes'
  ]
};

module.exports = sidebars;
