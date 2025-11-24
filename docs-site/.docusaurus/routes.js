import React from 'react';
import ComponentCreator from '@docusaurus/ComponentCreator';

export default [
  {
    path: '/__docusaurus/debug',
    component: ComponentCreator('/__docusaurus/debug', '5ff'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/config',
    component: ComponentCreator('/__docusaurus/debug/config', '5ba'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/content',
    component: ComponentCreator('/__docusaurus/debug/content', 'a2b'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/globalData',
    component: ComponentCreator('/__docusaurus/debug/globalData', 'c3c'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/metadata',
    component: ComponentCreator('/__docusaurus/debug/metadata', '156'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/registry',
    component: ComponentCreator('/__docusaurus/debug/registry', '88c'),
    exact: true
  },
  {
    path: '/__docusaurus/debug/routes',
    component: ComponentCreator('/__docusaurus/debug/routes', '000'),
    exact: true
  },
  {
    path: '/docs',
    component: ComponentCreator('/docs', '687'),
    routes: [
      {
        path: '/docs',
        component: ComponentCreator('/docs', 'a14'),
        routes: [
          {
            path: '/docs',
            component: ComponentCreator('/docs', '859'),
            routes: [
              {
                path: '/docs/',
                component: ComponentCreator('/docs/', 'be8'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/integrations/dotnet-aspnet',
                component: ComponentCreator('/docs/integrations/dotnet-aspnet', 'c73'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/integrations/java-spring-boot',
                component: ComponentCreator('/docs/integrations/java-spring-boot', 'aa1'),
                exact: true
              },
              {
                path: '/docs/integrations/node-express',
                component: ComponentCreator('/docs/integrations/node-express', '86a'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/integrations/overview',
                component: ComponentCreator('/docs/integrations/overview', 'ac0'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/module-mapping',
                component: ComponentCreator('/docs/module-mapping', '74c'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-configuration',
                component: ComponentCreator('/docs/modules/identity-configuration', '8c2'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-error-reference',
                component: ComponentCreator('/docs/modules/identity-error-reference', 'ca3'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-production-deployment',
                component: ComponentCreator('/docs/modules/identity-production-deployment', '018'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-token-generation',
                component: ComponentCreator('/docs/modules/identity-token-generation', '6d0'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-validator-dotnet',
                component: ComponentCreator('/docs/modules/identity-validator-dotnet', '1e6'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/identity-validator-nodejs',
                component: ComponentCreator('/docs/modules/identity-validator-nodejs', 'ef0'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/logging-configuration',
                component: ComponentCreator('/docs/modules/logging-configuration', '375'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/logging-dotnet',
                component: ComponentCreator('/docs/modules/logging-dotnet', '748'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/logging-enterprise-features',
                component: ComponentCreator('/docs/modules/logging-enterprise-features', '964'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/logging-nodejs',
                component: ComponentCreator('/docs/modules/logging-nodejs', '0df'),
                exact: true,
                sidebar: "docs"
              },
              {
                path: '/docs/modules/logging-targets',
                component: ComponentCreator('/docs/modules/logging-targets', 'a3b'),
                exact: true,
                sidebar: "docs"
              }
            ]
          }
        ]
      }
    ]
  },
  {
    path: '/',
    component: ComponentCreator('/', '2e1'),
    exact: true
  },
  {
    path: '*',
    component: ComponentCreator('*'),
  },
];
