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
            'modules/logging-enterprise-features',
            'modules/logging-targets'
          ]
        }
      ]
    }
  ]
};

module.exports = sidebars;
