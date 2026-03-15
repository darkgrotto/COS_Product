using CountOrSell.Api.Auth;
using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
    ?? "Host=localhost;Database=countorsell;Username=countorsell;Password=countorsell";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<ISerializedRepository, SerializedRepository>();
builder.Services.AddScoped<ISlabRepository, SlabRepository>();
builder.Services.AddScoped<ISealedInventoryRepository, SealedInventoryRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IGradingAgencyRepository, GradingAgencyRepository>();

// Auth services
builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
builder.Services.AddSingleton<IOAuthConfigService, OAuthConfigService>();
builder.Services.AddScoped<IUserService, UserService>();

// Cookie authentication (always available)
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/api/auth/login";
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// OAuth providers - only registered if configured
var googleClientId = builder.Configuration["OAuth:Google:ClientId"];
var googleClientSecret = builder.Configuration["OAuth:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var msClientId = builder.Configuration["OAuth:Microsoft:ClientId"];
var msClientSecret = builder.Configuration["OAuth:Microsoft:ClientSecret"];
if (!string.IsNullOrWhiteSpace(msClientId) && !string.IsNullOrWhiteSpace(msClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = msClientId;
        options.ClientSecret = msClientSecret;
    });
}

var ghClientId = builder.Configuration["OAuth:GitHub:ClientId"];
var ghClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(ghClientId) && !string.IsNullOrWhiteSpace(ghClientSecret))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = ghClientId;
        options.ClientSecret = ghClientSecret;
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? "healthy" : "unhealthy";
        var dbStatus = report.Entries.ContainsKey("AppDbContext") &&
            report.Entries["AppDbContext"].Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? "reachable" : "unreachable";

        if (report.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { status, database = dbStatus }));
    }
});

app.Run();

public partial class Program { }
