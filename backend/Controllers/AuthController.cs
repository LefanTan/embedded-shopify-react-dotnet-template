using System.Text;
using System.Web;
using backend.Config;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShopifySharp;
using ShopifySharp.Enums;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly bool IS_EMBEDDED = true;

    private readonly List<AuthorizationScope> _scopes =
        new()
        {
            AuthorizationScope.ReadProductListings,
            AuthorizationScope.ReadProducts,
            AuthorizationScope.ReadContent
        };

    private readonly ILogger<AuthController> _logger;
    private readonly IOptions<Settings> _settings;
    private readonly SessionService _sessionService;
    private readonly Services.WebhookService _webhookService;

    public AuthController(
        ILogger<AuthController> logger,
        IOptions<Settings> settings,
        Services.WebhookService webhookService,
        SessionService sessionService
    )
    {
        _logger = logger;
        _settings = settings;
        _sessionService = sessionService;
        _webhookService = webhookService;
    }

    [HttpGet]
    public IActionResult Auth()
    {
        var qs = Request.Query;

        // The name of the user's shop.
        var shop = qs["shop"].ToString();
        // Base64 encoded host name
        var host = qs["host"].ToString();
        var isEmbedded = qs["embedded"].ToString() == "1";

        // Store Url
        var storeUrl = $"{Request.Scheme}://{shop}";

        // Build the Shopify authorization URL
        var authUrl = AuthorizationService.BuildAuthorizationUrl(
            _scopes,
            storeUrl,
            _settings.Value.Shopify.ClientId,
            $"{Request.Scheme}://{Request.Host}{_settings.Value.Shopify.Auth.Callback}"
        );

        _logger.LogDebug($"Auth Query String: {Request.QueryString}");

        if (isEmbedded)
        {
            var u = new UriBuilder(
                $"{Request.Scheme}://{Request.Host}{_settings.Value.Shopify.Auth.ExitIframe}"
            );

            var redirectUriParams = new StringBuilder();
            redirectUriParams.Append($"shop={shop}&host={host}");

            var queryParams = new StringBuilder();
            queryParams.Append(redirectUriParams.ToString());

            queryParams.Append(
                $"&redirectUri={Request.Scheme}://{Request.Host}{_settings.Value.Shopify.Auth.Path}?{HttpUtility.UrlEncode(redirectUriParams.ToString())}"
            );

            u.Query = queryParams.ToString();

            _logger.LogInformation($"Embedded Redirect: {u.Uri}");

            // Render a React page that uses Shopify App Bridge redirect action, because we can't redirect from within an iframe
            return new RedirectResult(u.Uri.ToString(), false);
        }
        else
        {
            _logger.LogInformation($"Non Embedded Redirect: {authUrl}");

            return new RedirectResult(authUrl.ToString(), false);
        }
    }

    [HttpGet("callback", Name = nameof(Redirect))]
    public async Task<IActionResult> Callback()
    {
        var qs = Request.Query;

        // Authorization Grant Code, if exists
        var code = qs["code"];
        var shop = qs["shop"];
        var host = qs["host"];
        var storeUrl = $"{Request.Scheme}://{shop}";

        // Base64Decode is a custom extension method
        var decodedHost = host.ToString().Base64Decode();

        _logger.LogDebug($"Callback Query String: {Request.QueryString}");

        bool isValidDomain = await AuthorizationService.IsValidShopDomainAsync(storeUrl);

        if (!isValidDomain)
        {
            return BadRequest($"Invalid shop domain: {shop}");
        }

        try
        {
            // If code exists, exchange it for an access token
            if (!string.IsNullOrEmpty(code))
            {
                string accessToken = await AuthorizationService.Authorize(
                    code,
                    storeUrl,
                    _settings.Value.Shopify.ClientId,
                    _settings.Value.Shopify.ClientSecret
                );

                // Get the scopes for the access token
                var service = new AccessScopeService(storeUrl, accessToken);
                var scopes = await service.ListAsync();

                // Store the access token in the session
                _sessionService.SaveSession(
                    new Session
                    {
                        Id = SessionService.GetSessionId(shop!),
                        Shop = shop!,
                        Token = accessToken,
                        Scope = string.Join(", ", scopes.Select(s => s.Handle))
                    }
                );

                var storedSession = await _sessionService.GetSession(
                    SessionService.GetSessionId(shop!)
                );

                _logger.LogInformation($"Stored Session for shop: {storedSession?.Shop}");

                // Register webhooks
                await _webhookService.MountWebhookAsync(storeUrl, accessToken);
            }

            // If the app is supposed to be embedded, but this request isn't sent from an embedded app,
            // redirect to embedded app url.
            if (IS_EMBEDDED && qs["embedded"].ToString() != "1")
            {
                // Generate Embedded App Url
                var embeddedAppUrl =
                    $"{Request.Scheme}://{decodedHost}/apps/{_settings.Value.Shopify.ClientId}";

                _logger.LogInformation("Redirecting to Embedded App Url: " + embeddedAppUrl);
                return new RedirectResult(embeddedAppUrl);
            }
            else
            {
                var url = $"/?shop={shop}&host={HttpUtility.UrlEncode(host)}";

                _logger.LogInformation($"Rendering Frontend: {url}");
                return new RedirectResult(url, false);
            }
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
