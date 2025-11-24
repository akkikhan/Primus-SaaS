/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    {
      type: 'category',
      label: 'Modules',
      collapsible: true,
      items: [
        {
          type: 'category',
          label: 'Identity Validator',
          items: [
            'modules/identity-validator-dotnet',
            'modules/identity-validator-nodejs',
            'modules/identity-configuration',
            'modules/identity-token-generation',
            'modules/identity-tenant-resolver',
            'modules/identity-error-reference'
          ]
        },
        {
          type: 'category',
          label: 'Logging SDK',
          items: [
            'modules/logging-dotnet',
            'modules/logging-nodejs',
            'modules/logging-configuration',
            'modules/logging-middleware',
            'modules/logging-enterprise-features',
            'modules/logging-targets'
          ]
        }
      ]
    },
    'release-notes'
  ]
};

module.exports = sidebars;
