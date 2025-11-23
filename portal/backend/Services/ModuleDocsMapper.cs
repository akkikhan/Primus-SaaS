using PrimusSaaS.Portal.Api.Models;

namespace PrimusSaaS.Portal.Api.Services;

/// <summary>
/// Maps application stacks/modules to public documentation paths.
/// </summary>
public static class ModuleDocsMapper
{
    public static string GetIntegrationPath(AppStack stack)
    {
        return stack switch
        {
            AppStack.DotNet => "/docs/integrations/dotnet-aspnet",
            AppStack.NodeJS or AppStack.NodeJSNest or AppStack.TypeScriptLib => "/docs/integrations/node-express",
            AppStack.Python => "/docs/module-mapping",
            _ => "/docs/module-mapping"
        };
    }

    public static string GetUpdatePath(AppStack stack)
    {
        var basePath = GetIntegrationPath(stack);
        return $"{basePath}#update-steps";
    }
}
