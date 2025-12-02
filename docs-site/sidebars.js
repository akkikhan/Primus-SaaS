/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    {
      type: 'category',
      label: 'Identity Validator',
      collapsible: true,
      items: [
        'modules/identity-quick-start',
        'modules/identity-auth0',
        'modules/identity-azure-ad',
        'modules/identity-local-jwt',
        'modules/identity-multi-issuer',
        'modules/identity-advanced',
        'modules/identity-validator',
      ]
    },
    {
      type: 'category',
      label: 'Logging',
      collapsible: true,
      items: [
        'modules/logging-quick-start',
        'modules/logging-advanced',
        'modules/logging-module',
      ]
    },
    {
      type: 'category',
      label: 'Notifications',
      collapsible: true,
      items: [
        'modules/notifications-quick-start',
        'modules/notifications-advanced',
        'modules/notifications',
      ]
    },
    {
      type: 'category',
      label: 'Feature Flags',
      collapsible: true,
      items: [
        'modules/feature-flags-quick-start',
        'modules/feature-flags-advanced',
        'modules/feature-flags',
      ]
    },
    {
      type: 'category',
      label: 'Document Renderer',
      collapsible: true,
      items: [
        'modules/document-renderer-quick-start',
        'modules/document-renderer-advanced',
        'modules/document-renderer',
      ]
    },
    {
      type: 'category',
      label: 'Reference',
      collapsible: true,
      items: [
        'modules/live-demo-api',
        'modules/version-matrix',
      ]
    },
    'release-notes'
  ]
};

module.exports = sidebars;
