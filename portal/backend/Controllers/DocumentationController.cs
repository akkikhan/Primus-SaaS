using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/documentation")]
[Authorize(Roles = "Admin")]
public class DocumentationController : ControllerBase
{
    private readonly PortalDbContext _context;

    public DocumentationController(PortalDbContext context)
    {
        _context = context;
    }

    // GET: api/documentation/5
    [HttpGet("{applicationId}")]
    public async Task<ActionResult<DocumentationDto>> GenerateDocumentation(int applicationId)
    {
        var application = await _context.Applications
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.Module)
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.ModuleVersion)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
        {
            return NotFound();
        }

        var documentation = new DocumentationDto
        {
            ApplicationName = application.Name,
            PrimusClientId = application.PrimusClientId,
            Stack = application.Stack.ToString(),
            GeneratedAt = DateTime.UtcNow,
            Modules = application.ApplicationModules.Select(am => new DocumentationModuleDto
            {
                ModuleName = am.Module.Name,
                Version = am.ModuleVersion.Version,
                IsBreakingChange = am.ModuleVersion.IsBreakingChange,
                ReleaseNotes = am.ModuleVersion.ReleaseNotes,
                IntegrationSteps = GenerateIntegrationSteps(application.Stack.ToString(), am.Module.Name, am.ConfigJson),
                CodeSnippets = GenerateCodeSnippets(application.Stack.ToString(), am.Module.Name, application.PrimusClientId, am.ConfigJson)
            }).ToList()
        };

        return Ok(documentation);
    }

    // GET: api/documentation/{applicationId}/pdf
    [HttpGet("{applicationId}/pdf")]
    public async Task<IActionResult> DownloadDocumentationPdf(int applicationId)
    {
        var docResult = await GenerateDocumentation(applicationId);
        if (docResult.Result is NotFoundResult)
        {
            return NotFound();
        }

        var documentation = (docResult.Value as DocumentationDto)!;

        var pdfBytes = BuildPdf(documentation);
        var fileName = $"{documentation.ApplicationName}-PrimusDocs.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private List<string> GenerateIntegrationSteps(string stack, string moduleName, string configJson)
    {
        var steps = new List<string>();

        if (stack == "DotNet")
        {
            steps.Add($"Install NuGet package: Primus.SaaS.{moduleName}");
            steps.Add("Update appsettings.json with module configuration");
            steps.Add("Register middleware in Program.cs");
            steps.Add("Run your application and test the integration");
        }
        else if (stack == "NodeJS")
        {
            steps.Add($"Install NPM package: @primus-saas/{moduleName.ToLower()}");
            steps.Add("Create configuration file or environment variables");
            steps.Add("Import and configure middleware in your app entry point");
            steps.Add("Run your application and test the integration");
        }

        return steps;
    }

    private Dictionary<string, string> GenerateCodeSnippets(string stack, string moduleName, string primusClientId, string configJson)
    {
        var snippets = new Dictionary<string, string>();

        if (stack == "DotNet")
        {
            // appsettings.json snippet
            snippets["appsettings.json"] = GenerateDotNetConfig(moduleName, primusClientId, configJson);

            // Program.cs snippet
            snippets["Program.cs"] = GenerateDotNetProgramCs(moduleName);
        }
        else if (stack == "NodeJS")
        {
            // package.json snippet
            snippets["package.json"] = GenerateNodePackageJson(moduleName);

            // index.js snippet
            snippets["index.js"] = GenerateNodeIndexJs(moduleName, primusClientId, configJson);
        }

        return snippets;
    }

    private string GenerateDotNetConfig(string moduleName, string primusClientId, string configJson)
    {
        var sb = new StringBuilder();
        
        if (moduleName == "IdentityValidator")
        {
            // Parse configJson to extract issuer configurations
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();
            
            sb.AppendLine("{");
            sb.AppendLine("  \"PrimusIdentity\": {");
            sb.AppendLine("    \"Issuers\": [");
            
            // Generate issuer configurations from configJson
            // Expected format: { "issuers": [ { "name": "...", "type": "...", ... } ] }
            if (config.TryGetValue("issuers", out var issuersObj) && issuersObj is System.Text.Json.JsonElement issuersElement)
            {
                var issuers = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(issuersElement.GetRawText()) ?? new();
                
                for (int i = 0; i < issuers.Count; i++)
                {
                    var issuer = issuers[i];
                    sb.AppendLine("      {");
                    
                    foreach (var (key, value) in issuer)
                    {
                        var capitalizedKey = char.ToUpper(key[0]) + key.Substring(1);
                        
                        if (value is System.Text.Json.JsonElement jsonValue)
                        {
                            if (jsonValue.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                sb.AppendLine($"        \"{capitalizedKey}\": {jsonValue.GetRawText()},");
                            }
                            else if (jsonValue.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                sb.AppendLine($"        \"{capitalizedKey}\": \"{jsonValue.GetString()}\",");
                            }
                            else
                            {
                                sb.AppendLine($"        \"{capitalizedKey}\": {jsonValue.GetRawText()},");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"        \"{capitalizedKey}\": \"{value}\",");
                        }
                    }
                    
                    // Remove trailing comma from last property
                    var lastLine = sb.ToString().TrimEnd();
                    if (lastLine.EndsWith(","))
                    {
                        sb.Length -= Environment.NewLine.Length + 1;
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine(i < issuers.Count - 1 ? "      }," : "      }");
                }
            }
            else
            {
                // Fallback: Generate placeholder for Azure AD
                sb.AppendLine("      {");
                sb.AppendLine("        \"Name\": \"AzureAD\",");
                sb.AppendLine("        \"Type\": \"Oidc\",");
                sb.AppendLine("        \"Authority\": \"https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0\",");
                sb.AppendLine("        \"Issuer\": \"https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0\",");
                sb.AppendLine("        \"Audiences\": [\"api://YOUR-API-ID\"]");
                sb.AppendLine("      }");
            }
            
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine("}");
        }
        else
        {
            // Generic module configuration
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();
            sb.AppendLine("{");
            sb.AppendLine($"  \"Primus{moduleName}\": {{");
            sb.AppendLine($"    \"PrimusTrackingId\": \"{primusClientId}\",");

            foreach (var (key, value) in config)
            {
                sb.AppendLine($"    \"{key}\": \"{value}\",");
            }

            sb.AppendLine("  }");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private string GenerateDotNetProgramCs(string moduleName)
    {
        if (moduleName == "IdentityValidator")
        {
            return @"using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection(""PrimusIdentity"").Bind(options);
});

builder.Services.AddControllers();

var app = builder.Build();

// Use Primus Identity Validator (enables authentication)
app.UsePrimusIdentityValidator();
app.UseAuthorization();

app.MapControllers();
app.Run();";
        }
        else
        {
            return $@"using Primus.SaaS.{moduleName};

var builder = WebApplication.CreateBuilder(args);

// Add Primus {moduleName} middleware
builder.Services.AddPrimus{moduleName}(builder.Configuration.GetSection(""Primus{moduleName}""));

var app = builder.Build();

// Use Primus {moduleName}
app.UsePrimus{moduleName}();

app.Run();";
        }
    }

    private string GenerateNodePackageJson(string moduleName)
    {
        return $@"{{
  ""dependencies"": {{
    ""@primus-saas/{moduleName.ToLower()}"": ""^1.0.0""
  }}
}}";
    }

    private string GenerateNodeIndexJs(string moduleName, string primusClientId, string configJson)
    {
        var sb = new StringBuilder();
        
        // For IdentityValidator module, generate multi-issuer configuration
        if (moduleName == "IdentityValidator")
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();
            
            sb.AppendLine("const { primusIdentityValidator } = require('primus-identity-validator');");
            sb.AppendLine();
            sb.AppendLine("// Multi-Issuer Configuration");
            sb.AppendLine("const primusAuth = primusIdentityValidator({");
            sb.AppendLine("  issuers: [");
            
            // Generate issuer configurations from configJson
            if (config.TryGetValue("issuers", out var issuersObj) && issuersObj is System.Text.Json.JsonElement issuersElement)
            {
                var issuers = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(issuersElement.GetRawText()) ?? new();
                
                for (int i = 0; i < issuers.Count; i++)
                {
                    var issuer = issuers[i];
                    sb.AppendLine("    {");
                    
                    foreach (var (key, value) in issuer)
                    {
                        if (value is System.Text.Json.JsonElement jsonValue)
                        {
                            if (jsonValue.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                sb.AppendLine($"      {key}: {jsonValue.GetRawText()},");
                            }
                            else if (jsonValue.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                sb.AppendLine($"      {key}: '{jsonValue.GetString()}',");
                            }
                            else
                            {
                                sb.AppendLine($"      {key}: {jsonValue.GetRawText()},");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"      {key}: '{value}',");
                        }
                    }
                    
                    sb.AppendLine(i < issuers.Count - 1 ? "    }," : "    }");
                }
            }
            else
            {
                // Fallback: Generate placeholder for Azure AD
                sb.AppendLine("    {");
                sb.AppendLine("      name: 'AzureAD',");
                sb.AppendLine("      type: 'oidc',");
                sb.AppendLine("      authority: 'https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0',");
                sb.AppendLine("      issuer: 'https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0',");
                sb.AppendLine("      audiences: ['api://YOUR-API-ID']");
                sb.AppendLine("    }");
            }
            
            sb.AppendLine("  ]");
            sb.AppendLine("});");
            sb.AppendLine();
            sb.AppendLine("// Use in Express app");
            sb.AppendLine("app.use(primusAuth);");
            sb.AppendLine();
            sb.AppendLine("// Protected route example");
            sb.AppendLine("app.get('/api/protected', (req, res) => {");
            sb.AppendLine("  res.json({ ");
            sb.AppendLine("    user: req.primusUser,");
            sb.AppendLine("    tenant: req.tenantContext");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Generic module configuration (for future modules)
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();
            sb.AppendLine($"const {{ {moduleName} }} = require('@primus-saas/{moduleName.ToLower()}');");
            sb.AppendLine();
            sb.AppendLine($"const {moduleName.ToLower()}Config = {{");
            sb.AppendLine($"  primusTrackingId: '{primusClientId}',");

            foreach (var (key, value) in config)
            {
                sb.AppendLine($"  {key}: '{value}',");
            }

            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"// Initialize {moduleName}");
            sb.AppendLine($"const {moduleName.ToLower()}Middleware = {moduleName}.initialize({moduleName.ToLower()}Config);");
            sb.AppendLine();
            sb.AppendLine("// Use in Express app");
            sb.AppendLine($"app.use({moduleName.ToLower()}Middleware);");
        }

        return sb.ToString();
    }

    private byte[] BuildPdf(DocumentationDto documentation)
    {
        // Lightweight PDF rendering using QuestPDF with a concise layout
        QuestPDF.Settings.License = LicenseType.Community;

        var envVars = new[]
        {
            "PRIMUS_CLIENT_ID",
            "AZURE_TENANT_ID",
            "AZURE_CLIENT_ID",
            "AZURE_AUDIENCE"
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(documentation.ApplicationName).FontSize(20).SemiBold();
                        column.Item().Text($"Stack: {documentation.Stack}").FontSize(11).Light();
                        column.Item().Text($"Primus Client ID: {documentation.PrimusClientId}").FontSize(10);
                        column.Item().Text($"Generated: {documentation.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Text("Required Environment Variables").FontSize(13).SemiBold();
                    column.Item().Column(envColumn =>
                    {
                        foreach (var env in envVars)
                        {
                            envColumn.Item().Text(env).FontSize(11);
                        }
                    });

                    foreach (var module in documentation.Modules)
                    {
                        column.Item().Column(moduleColumn =>
                        {
                            moduleColumn.Item().Text($"{module.ModuleName} (v{module.Version})").FontSize(15).SemiBold();
                            moduleColumn.Item().Column(contentColumn =>
                            {
                                contentColumn.Spacing(6);
                                contentColumn.Item().Text(module.IsBreakingChange ? "⚠️ Breaking Change" : "Stable").FontColor(module.IsBreakingChange ? Colors.Red.Medium : Colors.Green.Darken2);
                                contentColumn.Item().Text($"Release Notes: {module.ReleaseNotes}").FontSize(11);

                                contentColumn.Item().Text("Integration Steps").FontSize(12).SemiBold();
                                contentColumn.Item().Column(stepsColumn =>
                                {
                                    foreach (var step in module.IntegrationSteps)
                                    {
                                        stepsColumn.Item().Text($"• {step}").FontSize(11);
                                    }
                                });

                                contentColumn.Item().Text("Code Snippets").FontSize(12).SemiBold();
                                foreach (var snippet in module.CodeSnippets)
                                {
                                    contentColumn.Item().Text(snippet.Key).FontSize(11).SemiBold();
                                    contentColumn.Item().Border(1).Padding(6).Background(Colors.Grey.Lighten4).Text(snippet.Value).FontSize(9);
                                }
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Primus SaaS • Integration Guide").FontSize(9);
                });
            });
        }).GeneratePdf();
    }
}

public record DocumentationDto
{
    public string ApplicationName { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public List<DocumentationModuleDto> Modules { get; init; } = new();
}

public record DocumentationModuleDto
{
    public string ModuleName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsBreakingChange { get; init; }
    public string ReleaseNotes { get; init; } = string.Empty;
    public List<string> IntegrationSteps { get; init; } = new();
    public Dictionary<string, string> CodeSnippets { get; init; } = new();
}
