using CountOrSell.Api.Auth;
using CountOrSell.Api.Background.AppVersion;
using CountOrSell.Api.Background.Backup;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Api.Background;
using CountOrSell.Api.Services;
using CountOrSell.Api.Services.Deployment;
using CountOrSell.Api.Services.Destinations;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Demo mode
builder.Services.AddSingleton<IDemoModeService, DemoModeService>();

// Session (used by demo mode for visitor_id)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(4);
});

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
builder.Services.AddScoped<ISealedProductRepository, SealedProductRepository>();
builder.Services.AddScoped<ISealedTaxonomyRepository, SealedTaxonomyRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IGradingAgencyRepository, GradingAgencyRepository>();

// Auth services
builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
builder.Services.AddSingleton<IOAuthConfigService, OAuthConfigService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();

// Card and data repositories
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IUserExportFileRepository, UserExportFileRepository>();

// Feature services
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddHttpClient<ITcgPlayerService, TcgPlayerService>();

// Image store
builder.Services.AddSingleton<IImageStore, FileSystemImageStore>();
builder.Services.AddHttpClient<ICardImageFetcher, ScryfallCardImageFetcher>();

// Update services
builder.Services.AddHttpClient<IUpdateManifestClient, UpdateManifestClient>();
builder.Services.AddHttpClient<IPackageDownloader, PackageDownloader>();
builder.Services.AddScoped<IPackageVerifier, PackageVerifier>();
builder.Services.AddScoped<IContentUpdateApplicator, ContentUpdateApplicator>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddHttpClient<IAppVersionService, AppVersionService>();
builder.Services.AddScoped<IUpdateRepository, UpdateRepository>();

// Cloud deployment service - provider selected by CLOUD_PROVIDER environment variable
var cloudProvider = Environment.GetEnvironmentVariable("CLOUD_PROVIDER") ?? string.Empty;
switch (cloudProvider.ToLowerInvariant())
{
    case "azure":
        builder.Services.AddSingleton<ICloudDeploymentService, AzureDeploymentService>();
        break;
    case "aws":
        builder.Services.AddSingleton<ICloudDeploymentService, AwsDeploymentService>();
        break;
    case "gcp":
        builder.Services.AddSingleton<ICloudDeploymentService, GcpDeploymentService>();
        break;
    default:
        builder.Services.AddSingleton<ICloudDeploymentService, NullDeploymentService>();
        break;
}

// Backup and restore services
builder.Services.AddScoped<IProcessRunner, ProcessRunner>();
builder.Services.AddScoped<ISchemaVersionService, SchemaVersionService>();
builder.Services.AddScoped<IBackupDestinationFactory, BackupDestinationFactory>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IRestoreService, RestoreService>();
builder.Services.AddScoped<IPreUpdateBackupService, PreUpdateBackupService>();
builder.Services.AddScoped<SchemaUpdateCoordinator>();

// Background services - StartupMigrationService first so it runs before other hosted services
builder.Services.AddHostedService<StartupMigrationService>();
builder.Services.AddSingleton<UpdateCheckService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UpdateCheckService>());
builder.Services.AddSingleton<IUpdateCheckTrigger>(sp => sp.GetRequiredService<UpdateCheckService>());
builder.Services.AddHostedService<AppVersionCheckService>();
builder.Services.AddHostedService<BackupScheduleService>();

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

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSession();
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

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
