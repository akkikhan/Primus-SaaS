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
        'modules/client-integration-guide'
      ]
    },
    'release-notes'
  ]
};

module.exports = sidebars;
