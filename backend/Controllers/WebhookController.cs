using backend.Authorization;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using ShopifySharp;

namespace backend.Controllers;

[Route("api/webhooks")]
[WebhookAuthorization]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;

    private readonly SessionService _sessionService;

    public WebhookController(ILogger<WebhookController> logger, SessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Perform any cleanups when app is uninstalled
    /// </summary>
    /// <param name="shopDomain"></param>
    /// <param name="shop"></param>
    [HttpPost("uninstalled")]
    public async Task<IActionResult> Uninstalled(
        [FromHeader(Name = "X-Shopify-Shop-Domain")] string shopDomain,
        [FromBody] Shop shop
    )
    {
        _logger.LogInformation("Uninstalled webhook received for {shop}", shopDomain);

        var sessionId = SessionService.GetSessionId(shopDomain);
        await _sessionService.DeleteSession(sessionId);

        _logger.LogInformation("Session deleted for {shop}", shopDomain);
        return Ok();
    }

    [HttpPost("products/update")]
    public IActionResult ProductUpdate(
        [FromHeader(Name = "X-Shopify-Shop-Domain")] string shopDomain,
        [FromBody] Product product
    )
    {
        _logger.LogInformation("Product update webhook received for {shop}", shopDomain);
        return Ok();
    }
}
