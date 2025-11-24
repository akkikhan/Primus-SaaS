/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    'module-mapping',
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
            'modules/identity-error-reference',
            'modules/identity-production-deployment'
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
    },
    {
      type: 'category',
      label: 'Integration Guides',
      collapsible: false,
      items: [
        'integrations/overview',
        'integrations/node-express',
        'integrations/dotnet-aspnet'
      ]
    }
  ]
};

module.exports = sidebars;
