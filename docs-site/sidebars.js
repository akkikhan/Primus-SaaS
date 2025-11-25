/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    {
      type: 'category',
      label: 'Modules',
      collapsible: true,
      items: [
        'modules/client-integration-guide'
      ]
    },
    'release-notes'
  ]
};

module.exports = sidebars;
