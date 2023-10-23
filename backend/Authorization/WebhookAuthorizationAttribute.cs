using backend.Config;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ShopifySharp;

namespace backend.Authorization;

public class WebhookAuthorizationAttribute : TypeFilterAttribute
{
    public WebhookAuthorizationAttribute() : base(typeof(WebhookAuthorizationFilter)) { }
}

public class WebhookAuthorizationFilter : Attribute, IAsyncAuthorizationFilter
{
    private readonly SessionService _sessionService;
    private readonly ILogger<WebhookAuthorizationFilter> _logger;
    private readonly IOptions<Settings> _settings;

    public WebhookAuthorizationFilter(
        SessionService sessionService,
        ILogger<WebhookAuthorizationFilter> logger,
        IOptions<Settings> settings
    )
    {
        _sessionService = sessionService;
        _logger = logger;
        _settings = settings;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        // Enable buffering, which lets the request stream be read multiple times
        request.EnableBuffering();

        var isAuthentic = await AuthorizationService.IsAuthenticWebhook(
            request.Headers,
            request.Body,
            _settings.Value.Shopify.ClientSecret
        );

        // Reset the request body stream position so the next middleware can read it
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        if (!isAuthentic)
        {
            _logger.LogWarning("Webhook is not authentic");
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}
