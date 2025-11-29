using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator;

public static class RefreshEndpointExtensions
{
    /// <summary>
    /// Maps a POST endpoint for exchanging refresh tokens.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="pattern">Route pattern, defaults to "/_primus/identity/refresh".</param>
    public static IEndpointRouteBuilder MapPrimusIdentityRefresh(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/_primus/identity/refresh")
    {
        endpoints.MapPost(pattern, async (RefreshRequest request, ITokenRefreshService refreshService, TokenRefreshOptions options) =>
        {
            if (!options.Enabled)
            {
                return Results.StatusCode(StatusCodes.Status404NotFound);
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { error = "refresh_token_required" });
            }

            var result = await refreshService.RefreshAsync(request.RefreshToken);
            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(new
            {
                access_token = result.AccessToken,
                refresh_token = result.NewRefreshToken,
                expires_at = result.ExpiresAt
            });
        }).WithDisplayName("Primus Identity Token Refresh");

        return endpoints;
    }

    private sealed record RefreshRequest(string RefreshToken);
}
