namespace backend.Config;

public class ShopifySettings
{
    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";

    public AuthSettings Auth { get; init; } = new AuthSettings();

    public WebhookSettings Webhook { get; init; } = new WebhookSettings();
}

public class AuthSettings
{
    public string Path { get; init; } = "";

    public string Callback { get; init; } = "";

    public string ExitIframe { get; init; } = "";
}

public class WebhookSettings
{
    public string Path { get; init; } = "";
}
