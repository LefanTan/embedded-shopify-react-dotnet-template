namespace backend.Config;

public class Settings
{
    public string BaseUrl { get; init; } = "";
    public ShopifySettings Shopify { get; init; } = new ShopifySettings();
}
