/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    'module-mapping',
    {
      type: 'category',
      label: 'Integration Guides',
      collapsible: false,
      items: [
        'integrations/overview',
        'integrations/node-express',
        'integrations/dotnet-aspnet',
        'integrations/java-spring-boot'
      ]
    }
  ]
};

module.exports = sidebars;
