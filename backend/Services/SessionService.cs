using System.Text.RegularExpressions;
using System.Web;
using backend.Config;
using backend.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using ShopifySharp;

namespace backend.Services;

public class SessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly IOptions<Settings> _settings;
    private readonly SessionContext _context;

    public SessionService(
        ILogger<SessionService> logger,
        IOptions<Settings> settings,
        SessionContext context
    )
    {
        _logger = logger;
        _settings = settings;
        _context = context;
    }

    /// <summary>
    /// Get the session id for a shop. This is used as the primary key for the session.
    /// </summary>
    /// <param name="shop"></param>
    /// <returns></returns>
    public static string GetSessionId(string shop)
    {
        return $"offline_{shop}";
    }

    /// <summary>
    /// Validate the shop domain and access token.
    /// </summary>
    public async Task ValidateShopInstalled(HttpContext context)
    {
        var shop = context.Request.Query["shop"].FirstOrDefault()?.ToString();

        _logger.LogInformation($"Validating session for shop: {shop}");

        // Validate the HMAC if exists and we're hitting the root path
        if (
            context.Request.Path.Value == "/"
            && context.Request.Query["hmac"].FirstOrDefault() != null
            && !AuthorizationService.IsAuthenticRequest(
                context.Request.Query,
                _settings.Value.Shopify.ClientSecret
            )
        )
        {
            throw new Exception($"Invalid HMAC for URL: {context.Request.GetDisplayUrl()}");
        }

        if (shop == null)
        {
            throw new Exception(
                $"Missing shop query string parameter, URL: {context.Request.GetDisplayUrl()}"
            );
        }

        var shopUrl = $"https://{shop}";
        var validShop = await AuthorizationService.IsValidShopDomainAsync(shopUrl);

        if (!validShop)
        {
            throw new Exception(
                $"Invalid shop domain: {shop}, URL: {context.Request.GetDisplayUrl()}"
            );
        }

        // Check if the store has installed the app before
        var sessionId = GetSessionId(shop);
        // Get the session from the database
        var session = await GetSession(sessionId);

        // Don't redirect via server if the user is trying to exit the iframe
        if (
            session == null
            && !Regex.IsMatch(context.Request.Path.Value!, _settings.Value.Shopify.Auth.ExitIframe)
        )
        {
            _logger.LogInformation(
                $"App installation was not found for shop, redirecting to {_settings.Value.Shopify.Auth.Path}{context.Request.QueryString}"
            );

            // Redirect to the auth page
            context.Response.Redirect(
                $"{_settings.Value.Shopify.Auth.Path}{context.Request.QueryString}"
            );
            return;
        }

        if (session == null)
        {
            _logger.LogInformation(
                $"Session not found for shop: {shop}, redirecting to {_settings.Value.Shopify.Auth.Path}{context.Request.QueryString}"
            );

            context.Response.Redirect(
                $"{_settings.Value.Shopify.Auth.Path}{context.Request.QueryString}"
            );
            return;
        }

        // Check if the access token is still valid by making a request to the shop
        var service = new AccessScopeService(shopUrl, session.Token);

        try
        {
            // Check if the access token is still valid by making a request to the shop
            // It is also good practice to make sure that the scope matches what we need, if not, re-authenticate
            await service.ListAsync();
        }
        catch (ShopifyException e)
        {
            if (e.Message.Contains("Invalid API key or access token"))
            {
                _logger.LogInformation($"Invalid/Outdated access token for shop: {shop}");
                context.Response.Redirect(
                    $"{_settings.Value.Shopify.Auth.Path}{context.Request.QueryString}"
                );
                return;
            }
        }

        // Set the CSP header, https://shopify.dev/docs/apps/store/security/iframe-protection
        context.Response.Headers.Add(
            "Content-Security-Policy",
            $"frame-ancestors https://{HttpUtility.HtmlEncode(shop)} https://admin.shopify.com;"
        );

        _logger.LogInformation($"Session found and active for shop: {shop}");

        return;
    }

    public async void SaveSession(Session session)
    {
        var existingSession = _context.Sessions.Find(session.Id);
        if (existingSession != null)
        {
            _context.Sessions.Remove(existingSession);
        }

        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task<Session?> GetSession(string id)
    {
        return await _context.Sessions.FindAsync(id);
    }

    public async Task<bool> DeleteSession(string id)
    {
        var session = _context.Sessions.Find(id);
        if (session != null)
        {
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
