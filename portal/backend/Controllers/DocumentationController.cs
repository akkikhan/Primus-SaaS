using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using System.Text;

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
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"Primus{moduleName}\": {{");
        sb.AppendLine($"    \"ClientId\": \"{primusClientId}\",");

        foreach (var (key, value) in config)
        {
            sb.AppendLine($"    \"{key}\": \"{value}\",");
        }

        sb.AppendLine("  }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateDotNetProgramCs(string moduleName)
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
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new();

        var sb = new StringBuilder();
        sb.AppendLine($"const {{ {moduleName} }} = require('@primus-saas/{moduleName.ToLower()}');");
        sb.AppendLine();
        sb.AppendLine($"const {moduleName.ToLower()}Config = {{");
        sb.AppendLine($"  clientId: '{primusClientId}',");

        foreach (var (key, value) in config)
        {
            sb.AppendLine($"  {key}: '{value}',");
        }

        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"// Initialize {moduleName}");
        sb.AppendLine($"const {moduleName.ToLower()}Middleware = {moduleName}.initialize({moduleName.ToLower()}Config);");
        sb.AppendLine();
        sb.AppendLine("// Use in Express app");
        sb.AppendLine($"app.use({moduleName.ToLower()}Middleware);");

        return sb.ToString();
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
