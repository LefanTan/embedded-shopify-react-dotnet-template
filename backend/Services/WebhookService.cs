using backend.Config;
using Microsoft.Extensions.Options;
using ShopifySharp;

namespace backend.Services;

public class WebhookService
{
    private readonly ILogger<WebhookService> _logger;

    private readonly IOptions<Settings> _settings;

    public WebhookService(ILogger<WebhookService> logger, IOptions<Settings> settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Mounts default webhook for newly installed app
    /// </summary>
    /// <param name="shopUrl"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task MountWebhookAsync(string shopUrl, string accessToken)
    {
        _logger.LogInformation($"Mounting webhook for shop: {shopUrl}");

        var service = new ShopifySharp.WebhookService(shopUrl, accessToken);
        var webhookBaseUrl = $"{_settings.Value.BaseUrl}{_settings.Value.Shopify.Webhook.Path}";

        // Available Topics: https://shopify.dev/docs/api/admin-rest/2023-10/resources/webhook#event-topics

        var hook = new Webhook()
        {
            Address = $"{webhookBaseUrl}/uninstalled",
            CreatedAt = DateTime.Now,
            Format = "json",
            Topic = "app/uninstalled",
        };

        await service.CreateAsync(hook);

        var productHook = new Webhook()
        {
            Address = $"{webhookBaseUrl}/products/update",
            CreatedAt = DateTime.Now,
            Format = "json",
            Topic = "products/update",
        };

        await service.CreateAsync(productHook);

        _logger.LogInformation($"Webhook mounted for shop: {shopUrl}");
    }
}
