using System.Text;
using backend.Authorization;
using backend.Config;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add Settings from appsettings.json
var settings = builder.Configuration.GetSection("Settings").Get<Settings>();
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

if (settings == null)
{
    throw new Exception("Settings not found in appsettings.json");
}

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SessionContext>();
builder.Services.AddScoped<SessionService>();

builder.Services.AddScoped<IAuthorizationMiddlewareResultHandler, ShopifyAuthorizationHandler>();

builder.Services
    .AddAuthentication(o =>
    {
        // Since this is a Shopify dedicated backend, we can set the default scheme to Shopify
        o.DefaultAuthenticateScheme = GlobalVariables.ShopifyAuthenticationScheme;
        o.DefaultChallengeScheme = GlobalVariables.ShopifyAuthenticationScheme;
        o.DefaultScheme = GlobalVariables.ShopifyAuthenticationScheme;
    })
    .AddJwtBearer(
        GlobalVariables.ShopifyAuthenticationScheme,
        o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(settings.Shopify.ClientSecret)
                ),
                ValidAudience = settings.Shopify.ClientId,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true, // Ensure for correct secret key
                ValidateLifetime = true, // Ensure token hasn't expired
                ValidateIssuer = false, // Issuer is the shop's domain, which we don't know beforehand
                ValidateActor = false, // Not used for shopify tokens
            };
        }
    );
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Serve index.html for "/"
app.MapGet("/", HandleIndex);

// Catch all route
app.MapFallback(HandleIndex);

async Task HandleIndex(HttpContext context, SessionService sessionService)
{
    await sessionService.ValidateShopInstalled(context);

    // Serve the index.html file
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
}

app.Run();
