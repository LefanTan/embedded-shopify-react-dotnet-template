using backend.Config;
using backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopifySharp;

namespace backend.Controllers;

[Route("api/products")]
[ApiController]
[Authorize]
public class ProductController : ControllerBase
{
    public ProductController() { }

    /// <summary>
    /// Get a list of products for the shop.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accessToken = (Session?)HttpContext.Items[GlobalVariables.TokenContextKey];
        var shopUrl = (string?)HttpContext.Items[GlobalVariables.ShopUrlContextKey];

        if (accessToken == null)
        {
            return Unauthorized("Missing access token from context");
        }

        var service = new ProductService(shopUrl, accessToken.Token);
        var products = await service.ListAsync();

        return Ok(products);
    }
}
