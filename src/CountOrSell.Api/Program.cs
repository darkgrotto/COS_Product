using CountOrSell.Api;
using CountOrSell.Api.Auth;
using CountOrSell.Api.Background.AppVersion;
using CountOrSell.Api.Background.Backup;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Api.Background;
using CountOrSell.Api.Services;
using CountOrSell.Api.Services.Deployment;
using CountOrSell.Api.Services.Destinations;
using CountOrSell.Api.Services.LogForwarding;
using CountOrSell.Api.Services.Signing;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

dotenv.net.DotEnv.Load(new dotenv.net.DotEnvOptions(ignoreExceptions: true));

var builder = WebApplication.CreateBuilder(args);

// PORT env var drives Kestrel's listen address. ConfigureKestrel has the highest
// priority - it overrides ASPNETCORE_URLS, ASPNETCORE_HTTP_PORTS, and UseUrls,
// so the base Docker image's ASPNETCORE_HTTP_PORTS=8080 cannot conflict.
// ASPNETCORE_URLS still wins if someone explicitly sets it (checked first).
if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is null)
{
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "3000");
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
}

// Trust X-Forwarded-Proto / X-Forwarded-For from the reverse proxy in front of
// Kestrel (nginx for Docker; App Service / App Runner / Cloud Run for cloud
// deployments). Without this the app sees every request as plain http, which
// means SameAsRequest cookies won't carry Secure, breaks redirect_uri scheme matching
// on OAuth, and yields the proxy's IP in audit logs instead of the client's.
// KnownProxies/KnownNetworks are cleared because the proxy address is not
// predictable inside container networks.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Anti-forgery (CSRF) defense-in-depth. Cookie-based auth already has SameSite=Strict,
// but tokens close the gap for older clients and any future XSS. The SPA reads the
// token from GET /api/auth/csrf and echoes it back as X-CSRF-TOKEN on every state-
// changing request; the global filter validates non-safe HTTP methods.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
});

// AddControllersWithViews (not AddControllers) so view services register
// AutoValidateAntiforgeryTokenAuthorizationFilter - the internal filter
// type that AutoValidateAntiforgeryTokenAttribute resolves at runtime.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

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

// Database. Resolved once at startup from (in priority order):
// 1. ConnectionStrings:Default from appsettings
// 2. POSTGRES_CONNECTION env var (full connection string)
// 3. Individual env vars: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
// The resolved string is stored back into IConfiguration so downstream services
// (BackupService, RestoreService, etc.) read it via GetConnectionString("Default")
// without re-resolving.
var connectionString = ResolveConnectionString(builder.Configuration);

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Database connection string is not configured. Set DB_USER and DB_PASSWORD " +
        "(env vars), POSTGRES_CONNECTION (full connection string), or " +
        "ConnectionStrings:Default (configuration) before starting the API.");

var inMemoryOverrides = new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = connectionString
};

if (string.IsNullOrEmpty(builder.Configuration["SETUP_TOKEN"]))
{
    inMemoryOverrides["SETUP_TOKEN"] = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
}

// The documented flat OAUTH_* env vars alias the hierarchical OAuth:* keys the
// auth handlers read. An explicit OAuth:* value (env var double-underscore form
// or appsettings) wins over its alias. Resolved before the app_settings DB
// source is inserted, so env aliases still override admin-UI-saved values.
var oauthEnvAliases = new (string EnvKey, string ConfigKey)[]
{
    ("OAUTH_GOOGLE_CLIENT_ID", "OAuth:Google:ClientId"),
    ("OAUTH_GOOGLE_CLIENT_SECRET", "OAuth:Google:ClientSecret"),
    ("OAUTH_MICROSOFT_CLIENT_ID", "OAuth:Microsoft:ClientId"),
    ("OAUTH_MICROSOFT_CLIENT_SECRET", "OAuth:Microsoft:ClientSecret"),
    ("OAUTH_MICROSOFTENTRA_CLIENT_ID", "OAuth:MicrosoftEntra:ClientId"),
    ("OAUTH_MICROSOFTENTRA_CLIENT_SECRET", "OAuth:MicrosoftEntra:ClientSecret"),
    ("OAUTH_MICROSOFTENTRA_TENANT_ID", "OAuth:MicrosoftEntra:TenantId"),
    ("OAUTH_GITHUB_CLIENT_ID", "OAuth:GitHub:ClientId"),
    ("OAUTH_GITHUB_CLIENT_SECRET", "OAuth:GitHub:ClientSecret"),
};
foreach (var (envKey, configKey) in oauthEnvAliases)
{
    if (!string.IsNullOrWhiteSpace(builder.Configuration[envKey])
        && string.IsNullOrWhiteSpace(builder.Configuration[configKey]))
    {
        inMemoryOverrides[configKey] = builder.Configuration[envKey];
    }
}

builder.Configuration.AddInMemoryCollection(inMemoryOverrides);

// DbContextOptions registered as Singleton so the singleton IDbContextFactory
// can consume it; AppDbContext itself stays Scoped.
builder.Services.AddDbContext<AppDbContext>(
    options => options.UseNpgsql(connectionString),
    optionsLifetime: ServiceLifetime.Singleton);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString), ServiceLifetime.Singleton);

// Merge admin-managed settings from the app_settings table into IConfiguration.
// Inserted at index 0 so env vars and appsettings still override DB values.
// Auth handlers (Google / Microsoft / Entra / GitHub) read from IConfiguration
// at startup, so changes saved via the admin UI take effect on next restart.
builder.Configuration.Sources.Insert(0,
    new DbAppSettingsConfigurationSource { ConnectionString = connectionString });

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

// Audit log
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Reference-data validators
builder.Services.AddSingleton<ITreatmentValidator, TreatmentValidator>();
builder.Services.AddSingleton<IImageStatsService, ImageStatsService>();

// Feature services
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICollectionImportExportService, CollectionImportExportService>();
builder.Services.AddScoped<IWishlistImportExportService, WishlistImportExportService>();
builder.Services.AddScoped<ISerializedImportExportService, SerializedImportExportService>();
builder.Services.AddScoped<ISlabImportExportService, SlabImportExportService>();
builder.Services.AddScoped<ISealedInventoryImportExportService, SealedInventoryImportExportService>();
builder.Services.AddHttpClient<ITcgPlayerService, TcgPlayerService>();

// Image store
builder.Services.AddSingleton<IImageStore, FileSystemImageStore>();
builder.Services.AddScoped<IAvatarService, AvatarService>();
builder.Services.AddHttpClient<ICardImageFetcher, ScryfallCardImageFetcher>();

// Update services
builder.Services.AddHttpClient<IUpdateManifestClient, UpdateManifestClient>();
builder.Services.AddHttpClient<IPackageDownloader, PackageDownloader>();
builder.Services.AddScoped<IPackageVerifier, PackageVerifier>();

// Manifest signing: JWKS provider is a singleton (in-memory cache + DB persistence);
// the verifier is scoped so it picks up the singleton without holding state itself.
builder.Services.AddHttpClient(name: "Jwks", c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<IJwksProvider, JwksProvider>();
builder.Services.AddScoped<IManifestSignatureVerifier, ManifestSignatureVerifier>();
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

// Named HTTP client for fetching image blobs from update packages
builder.Services.AddHttpClient("ImageFetch", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Log forwarding
builder.Services.AddSingleton<LogForwardingConfigHolder>();
builder.Services.AddSingleton<HttpLogForwardingProvider>();
builder.Services.AddHttpClient("LogForwarding");

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
builder.Services.AddHostedService<JwksRefreshService>();

// Cookie authentication (always available)
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
    })
    // Intermediate cookie the OAuth/OIDC handlers sign the external principal
    // into (SignInScheme below). Bare [Authorize] authenticates only against
    // the main cookie scheme, so this ticket grants no application access;
    // the callback endpoint exchanges it for the main cookie only after the
    // external identity maps to an active application user.
    .AddCookie(OAuthProviders.ExternalScheme, options =>
    {
        options.Cookie.Name = "CountOrSell.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        // Lax, not Strict: the callback redirect chain is initiated by the
        // provider (cross-site), and Strict would drop the cookie on the
        // top-level GET back into the app.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
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
        options.SignInScheme = OAuthProviders.ExternalScheme;
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
        options.SignInScheme = OAuthProviders.ExternalScheme;
    });
}

// Microsoft Entra ID (work / school accounts). Distinct from MicrosoftAccount which
// only covers consumer (Live) accounts. TenantId selects the directory:
// a specific GUID for single-tenant, "common" for any tenant + personal,
// "organizations" for any tenant, "consumers" for personal only.
var entraClientId = builder.Configuration["OAuth:MicrosoftEntra:ClientId"];
var entraClientSecret = builder.Configuration["OAuth:MicrosoftEntra:ClientSecret"];
var entraTenantId = builder.Configuration["OAuth:MicrosoftEntra:TenantId"];
if (!string.IsNullOrWhiteSpace(entraClientId)
    && !string.IsNullOrWhiteSpace(entraClientSecret)
    && !string.IsNullOrWhiteSpace(entraTenantId))
{
    authBuilder.AddOpenIdConnect("microsoft-entra", "Microsoft (Entra ID)", options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{entraTenantId}/v2.0";
        options.ClientId = entraClientId;
        options.ClientSecret = entraClientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.CallbackPath = "/signin-microsoft-entra";
        options.SignInScheme = OAuthProviders.ExternalScheme;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
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
        options.SignInScheme = OAuthProviders.ExternalScheme;
    });
}

builder.Services.AddAuthorization();

// Throttle unauthenticated credential-submission endpoints (login, first-run setup)
// per client IP to blunt online password brute-forcing. Generous enough not to affect
// real users. Note: behind a proxy this partitions on the forwarded client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Load persisted log forwarding config and register the logger provider.
// Wrapped in try/catch so a missing or empty DB (first run) doesn't block startup.
try
{
    using var startupScope = app.Services.CreateScope();
    var startupDb = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configHolder = app.Services.GetRequiredService<LogForwardingConfigHolder>();
    var settings = startupDb.AppSettings
        .Where(s => s.Key.StartsWith("log_forwarding."))
        .ToDictionary(s => s.Key, s => s.Value);
    settings.TryGetValue("log_forwarding.enabled", out var lfEnabled);
    settings.TryGetValue("log_forwarding.url", out var lfUrl);
    settings.TryGetValue("log_forwarding.auth_header", out var lfAuth);
    settings.TryGetValue("log_forwarding.min_level", out var lfLevel);
    configHolder.Update(new LogForwardingConfig
    {
        Enabled = lfEnabled == "true",
        DestinationUrl = string.IsNullOrEmpty(lfUrl) ? null : lfUrl,
        AuthHeader = string.IsNullOrEmpty(lfAuth) ? null : lfAuth,
        MinLevel = string.IsNullOrEmpty(lfLevel) ? "Warning" : lfLevel
    });
}
catch { /* DB not yet available - log forwarding stays disabled until first config save */ }

app.Services.GetRequiredService<ILoggerFactory>()
    .AddProvider(app.Services.GetRequiredService<HttpLogForwardingProvider>());

// Surface a clear warning if the canonical public URL is not pinned. Without it,
// invite emails fall back to the incoming Host header, which is attacker-controllable.
if (string.IsNullOrWhiteSpace(app.Configuration[PublicBaseUrlResolver.ConfigKey]))
{
    app.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup")
        .LogWarning(
            "{Key} is not configured. Invite-link generation will fall back to the " +
            "incoming HTTP Host header, which is vulnerable to host-header injection. " +
            "Set {Key} to your canonical public origin (e.g. https://app.example.com).",
            PublicBaseUrlResolver.ConfigKey, PublicBaseUrlResolver.ConfigKey);
}

// Must run before anything that reads Request.Scheme or Request.IsHttps
// (auth, antiforgery, static-file redirects).
app.UseForwardedHeaders();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
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

public partial class Program
{
    internal static string? ResolveConnectionString(IConfiguration config)
    {
        if (config.GetConnectionString("Default") is { Length: > 0 } cs)
            return cs;

        if (config["POSTGRES_CONNECTION"] is { Length: > 0 } envCs)
            return envCs;

        var user = config["DB_USER"];
        var pass = config["DB_PASSWORD"];
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            return null;

        var host = config["DB_HOST"] ?? "localhost";
        var port = config["DB_PORT"] ?? "5432";
        var name = config["DB_NAME"] ?? "countorsell";
        return $"Host={host};Port={port};Database={name};Username={user};Password={pass}";
    }
}
