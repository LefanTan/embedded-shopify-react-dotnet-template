namespace backend.Authorization;

using System.Text.RegularExpressions;
using backend.Config;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

public class ShopifyAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    private readonly ILogger<ShopifyAuthorizationHandler> _logger;
    private readonly SessionService _sessionService;

    public ShopifyAuthorizationHandler(
        SessionService sessionService,
        ILogger<ShopifyAuthorizationHandler> logger
    )
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        if (!authorizeResult.Succeeded)
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        _logger.LogDebug("Handling authorization result: {result}", context.User.Claims);

        var claims = context.User.Claims;

        // dest = shop
        var dest = claims.FirstOrDefault((c) => c.Type == "dest")?.Value;

        if (string.IsNullOrEmpty(dest))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Missing dest claim", });
            return;
        }

        var shop = Regex.Replace(dest, "https?://", "");
        var sessionId = SessionService.GetSessionId(shop);

        var session = await _sessionService.GetSession(sessionId);

        if (session == null)
        {
            var redirectUri =
                $"{context.Request.Scheme}://{context.Request.Host}/api/auth?shop={shop}";
            _logger.LogInformation(
                "Session not found for shop {shop}, redirecting to {redirectUri}",
                shop,
                redirectUri
            );

            context.Response.Redirect(redirectUri);
            return;
        }

        // Inject session into context
        context.Items[GlobalVariables.TokenContextKey] = session;
        context.Items[GlobalVariables.ShopUrlContextKey] = $"{context.Request.Scheme}://{shop}";

        // Fall back to the default implementation.
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
