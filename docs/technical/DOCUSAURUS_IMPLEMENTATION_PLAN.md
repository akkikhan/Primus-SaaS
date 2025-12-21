# Docusaurus Documentation & Email Integration Plan

## Overview
Create a Docusaurus-based documentation site that serves independent module documentation and integrates with the portal's email notification system.

## Architecture

### 1. Documentation Structure
```
docs/
├── identity-validator/
│   ├── dotnet/
│   │   ├── quick-start.md
│   │   ├── installation.md
│   │   ├── configuration.md
│   │   ├── token-generation.md
│   │   ├── error-reference.md
│   │   ├── production-deployment.md
│   │   └── api-reference.md
│   └── nodejs/
│       ├── quick-start.md
│       ├── installation.md
│       ├── configuration.md
│       ├── token-generation.md
│       ├── error-reference.md
│       └── production-deployment.md
├── logging/
│   ├── dotnet/
│   │   ├── quick-start.md
│   │   ├── installation.md
│   │   ├── configuration.md
│   │   ├── enterprise-features.md
│   │   ├── targets.md
│   │   └── api-reference.md
│   └── nodejs/
│       ├── quick-start.md
│       ├── installation.md
│       ├── configuration.md
│       ├── enterprise-features.md
│       └── targets.md
└── getting-started.md
```

### 2. Docusaurus Configuration

**docusaurus.config.js**:
```javascript
module.exports = {
  title: 'Primus SaaS Documentation',
  tagline: 'Enterprise SDKs for .NET and Node.js',
  url: 'https://docs.primus-saas.com',
  baseUrl: '/',
  
  themeConfig: {
    navbar: {
      title: 'Primus SaaS',
      items: [
        {
          type: 'doc',
          docId: 'getting-started',
          label: 'Getting Started',
        },
        {
          type: 'dropdown',
          label: 'Identity Validator',
          items: [
            { label: '.NET', to: '/docs/identity-validator/dotnet/quick-start' },
            { label: 'Node.js', to: '/docs/identity-validator/nodejs/quick-start' },
          ],
        },
        {
          type: 'dropdown',
          label: 'Logging',
          items: [
            { label: '.NET', to: '/docs/logging/dotnet/quick-start' },
            { label: 'Node.js', to: '/docs/logging/nodejs/quick-start' },
          ],
        },
      ],
    },
  },
  
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
        },
      },
    ],
  ],
};
```

### 3. Email Integration

When a module is assigned to a client application, the portal sends an email with the appropriate documentation link.

**Backend: ApplicationsController.cs**
```csharp
[HttpPost("{id}/assign-module")]
public async Task<IActionResult> AssignModule(string id, [FromBody] AssignModuleRequest request)
{
    var application = await _context.Applications.FindAsync(id);
    if (application == null) return NotFound();

    // Assign module
    var assignment = new ModuleAssignment
    {
        ApplicationId = id,
        ModuleName = request.ModuleName,
        Stack = request.Stack, // "dotnet" or "nodejs"
        AssignedAt = DateTime.UtcNow
    };

    _context.ModuleAssignments.Add(assignment);
    await _context.SaveChangesAsync();

    // Generate documentation link
    var docLink = GenerateDocumentationLink(request.ModuleName, request.Stack);

    // Send email
    await _emailService.SendModuleAssignedEmail(new ModuleAssignedEmailData
    {
        To = application.ClientEmail,
        ApplicationName = application.Name,
        ModuleName = request.ModuleName,
        Stack = request.Stack,
        DocumentationLink = docLink,
        PackageName = GetPackageName(request.ModuleName, request.Stack),
        InstallCommand = GetInstallCommand(request.ModuleName, request.Stack)
    });

    return Ok(assignment);
}

private string GenerateDocumentationLink(string moduleName, string stack)
{
    var baseUrl = "https://docs.primus-saas.com/docs";
    var module = moduleName.ToLowerInvariant().Replace(" ", "-");
    
    return $"{baseUrl}/{module}/{stack}/quick-start";
}

private string GetPackageName(string moduleName, string stack)
{
    return (moduleName, stack) switch
    {
        ("Identity Validator", "dotnet") => "PrimusSaaS.Identity.Validator",
        ("Identity Validator", "nodejs") => "primus-identity-validator",
        ("Logging", "dotnet") => "PrimusSaaS.Logging",
        ("Logging", "nodejs") => "@primus-saas/logging",
        _ => "Unknown"
    };
}

private string GetInstallCommand(string moduleName, string stack)
{
    var packageName = GetPackageName(moduleName, stack);
    
    return stack switch
    {
        "dotnet" => $"dotnet add package {packageName}",
        "nodejs" => $"npm install {packageName}",
        _ => ""
    };
}
```

### 4. Email Template

**ModuleAssignedEmail.html**:
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #4CAF50; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .code-block { background: #2d2d2d; color: #f8f8f2; padding: 15px; border-radius: 5px; font-family: monospace; }
        .button { display: inline-block; padding: 12px 24px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🎉 Module Assigned!</h1>
        </div>
        <div class="content">
            <h2>Hello!</h2>
            <p>The <strong>{{ModuleName}}</strong> module has been assigned to your application <strong>{{ApplicationName}}</strong>.</p>
            
            <h3>📦 Installation</h3>
            <div class="code-block">{{InstallCommand}}</div>
            
            <h3>📚 Documentation</h3>
            <p>Get started with our comprehensive integration guide:</p>
            <a href="{{DocumentationLink}}" class="button">View Documentation →</a>
            
            <h3>🚀 Quick Start</h3>
            <p>The documentation includes:</p>
            <ul>
                <li>Installation instructions</li>
                <li>Configuration examples</li>
                <li>Code samples for {{Stack}}</li>
                <li>Best practices</li>
                <li>Troubleshooting guide</li>
            </ul>
            
            <p>Need help? Visit our <a href="https://docs.primus-saas.com">documentation</a> or contact support.</p>
        </div>
    </div>
</body>
</html>
```

### 5. Implementation Steps

#### Phase 1: Docusaurus Setup
1. Create `docs/` directory in repository root
2. Initialize Docusaurus: `npx create-docusaurus@latest docs classic`
3. Configure `docusaurus.config.js` with module structure
4. Create `sidebars.js` for navigation

#### Phase 2: Documentation Migration
1. Convert existing README.md files to Docusaurus markdown
2. Split large docs into logical sections
3. Add code examples with syntax highlighting
4. Create interactive examples where possible

#### Phase 3: Backend Integration
1. Update `ApplicationsController.cs` with email logic
2. Create `ModuleAssignedEmail.html` template
3. Update `EmailService.cs` with new method
4. Add configuration for documentation base URL

#### Phase 4: Deployment
1. Build Docusaurus: `npm run build`
2. Deploy to `docs.primus-saas.com` (Vercel/Netlify/Azure Static Web Apps)
3. Configure DNS
4. Test email links

### 6. Documentation URL Structure

```
https://docs.primus-saas.com/
├── /docs/getting-started
├── /docs/identity-validator/
│   ├── dotnet/
│   │   ├── quick-start
│   │   ├── installation
│   │   ├── configuration
│   │   ├── token-generation
│   │   ├── error-reference
│   │   └── production-deployment
│   └── nodejs/
│       └── (same structure)
└── /docs/logging/
    ├── dotnet/
    │   ├── quick-start
    │   ├── installation
    │   ├── configuration
    │   ├── enterprise-features
    │   └── targets
    └── nodejs/
        └── (same structure)
```

### 7. Benefits

1. **Single Source of Truth**: All documentation in one place
2. **Version Control**: Docs versioned with code
3. **Search**: Built-in search functionality
4. **Responsive**: Mobile-friendly
5. **Fast**: Static site generation
6. **SEO**: Optimized for search engines
7. **Automated**: Email links always point to correct docs

### 8. Next Steps

1. Create Docusaurus project structure
2. Migrate existing documentation
3. Implement email integration in portal
4. Deploy documentation site
5. Test end-to-end flow

## Conclusion

This architecture ensures that:
- Each module has independent, comprehensive documentation
- Clients receive direct links to relevant docs based on their stack
- Documentation is always up-to-date and version-controlled
- The system scales easily as new modules are added
