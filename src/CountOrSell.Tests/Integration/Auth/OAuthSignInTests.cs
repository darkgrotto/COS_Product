using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CountOrSell.Api.Auth;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CountOrSell.Tests.Integration.Auth;

/// <summary>
/// Replaces the External cookie scheme so tests can inject an "external"
/// principal (the result of a provider round-trip) via request headers:
/// X-Ext-Sub (subject id, required), X-Ext-Provider, X-Ext-Mode,
/// X-Ext-Link-User, X-Ext-Email, X-Ext-Name. The main application cookie
/// scheme stays real, so the callback's cookie issuance is exercised
/// end-to-end.
/// </summary>
public class HeaderDrivenExternalHandler : SignOutAuthenticationHandler<AuthenticationSchemeOptions>
{
    public HeaderDrivenExternalHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Ext-Sub", out var sub))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, sub.ToString()) };
        if (Request.Headers.TryGetValue("X-Ext-Email", out var email))
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        if (Request.Headers.TryGetValue("X-Ext-Name", out var name))
            claims.Add(new Claim(ClaimTypes.Name, name.ToString()));

        var properties = new AuthenticationProperties();
        if (Request.Headers.TryGetValue("X-Ext-Provider", out var provider))
            properties.Items["cos_provider"] = provider.ToString();
        if (Request.Headers.TryGetValue("X-Ext-Mode", out var mode))
            properties.Items["cos_mode"] = mode.ToString();
        if (Request.Headers.TryGetValue("X-Ext-Link-User", out var linkUser))
            properties.Items["cos_link_user"] = linkUser.ToString();

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties) =>
        Task.CompletedTask;
}

public class OAuthSignInTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "correct-horse-battery-staple";

    private readonly WebApplicationFactory<Program> _factory;

    public OAuthSignInTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(string dbName)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            // Google is the only configured provider in tests.
            builder.UseSetting("OAuth:Google:ClientId", "test-client-id");
            builder.UseSetting("OAuth:Google:ClientSecret", "test-client-secret");

            builder.ConfigureServices(services =>
            {
                var desc = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (desc != null) services.Remove(desc);
                var ctxDesc = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AppDbContext));
                if (ctxDesc != null) services.Remove(ctxDesc);

                services.AddDbContext<AppDbContext>(
                    opt => opt.UseInMemoryDatabase(dbName),
                    optionsLifetime: ServiceLifetime.Singleton);

                // Swap only the External scheme's handler; every other scheme
                // (main cookie, Google) stays as Program.cs registered it.
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    var scheme = o.Schemes.Single(s => s.Name == OAuthProviders.ExternalScheme);
                    scheme.HandlerType = typeof(HeaderDrivenExternalHandler);
                });
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task<User> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string username,
        AccountState state = AccountState.Active,
        string? oauthProvider = null,
        string? oauthSub = null,
        bool withPassword = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var localAuth = scope.ServiceProvider.GetRequiredService<CountOrSell.Api.Auth.ILocalAuthService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = username,
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            State = state,
            PasswordHash = withPassword ? localAuth.HashPassword(Password) : null,
            OAuthProvider = oauthProvider,
            OAuthProviderUserId = oauthSub,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsync("/api/auth/login", new StringContent(
            JsonSerializer.Serialize(new { username, password = Password }),
            Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private static async Task AddCsrfTokenAsync(HttpClient client)
    {
        var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", doc.RootElement.GetProperty("token").GetString());
    }

    private static HttpRequestMessage CallbackRequest(
        string provider,
        string? sub,
        string? challengedProvider,
        string? mode = null,
        string? linkUser = null,
        string? email = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/auth/oauth/{provider}/callback");
        if (sub is not null) request.Headers.Add("X-Ext-Sub", sub);
        if (challengedProvider is not null) request.Headers.Add("X-Ext-Provider", challengedProvider);
        if (mode is not null) request.Headers.Add("X-Ext-Mode", mode);
        if (linkUser is not null) request.Headers.Add("X-Ext-Link-User", linkUser);
        if (email is not null) request.Headers.Add("X-Ext-Email", email);
        return request;
    }

    private static async Task<User?> ReloadUserAsync(WebApplicationFactory<Program> factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    // ---- Provider listing and challenge routing ----

    [Fact]
    public async Task ProvidersEndpoint_ListsOnlyConfiguredProviders()
    {
        using var factory = BuildFactory(nameof(ProvidersEndpoint_ListsOnlyConfiguredProviders));
        var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/oauth/providers");

        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();
        Assert.Equal(new[] { "google" }, ids);
    }

    [Fact]
    public async Task FlatOAuthEnvAliases_ConfigureProvider()
    {
        // The documented flat OAUTH_* keys must reach the OAuth:* config keys
        // the handlers and the provider list read.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OAUTH_GITHUB_CLIENT_ID", "alias-client-id");
            builder.UseSetting("OAUTH_GITHUB_CLIENT_SECRET", "alias-client-secret");
        });
        var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/oauth/providers");

        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();
        Assert.Contains("github", ids);
    }

    [Fact]
    public async Task OAuthLogin_Google_RedirectsToGoogleAuthorizeEndpoint()
    {
        using var factory = BuildFactory(nameof(OAuthLogin_Google_RedirectsToGoogleAuthorizeEndpoint));
        var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/oauth/google");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://accounts.google.com/", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("facebook")]
    [InlineData("github")] // known provider, but not configured in the test factory
    [InlineData("google%20x")]
    public async Task OAuthLogin_UnknownOrUnconfiguredProvider_IsRejected(string provider)
    {
        using var factory = BuildFactory($"login-reject-{provider}");
        var client = CreateClient(factory);

        var response = await client.GetAsync($"/api/auth/oauth/{provider}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OAuthLinkChallenge_RequiresAuthentication()
    {
        using var factory = BuildFactory(nameof(OAuthLinkChallenge_RequiresAuthentication));
        var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/oauth/google/link");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Sign-in callback ----

    [Fact]
    public async Task Callback_UnknownIdentity_RedirectsToNotProvisioned_AndNotifiesAdminOnce()
    {
        using var factory = BuildFactory(nameof(Callback_UnknownIdentity_RedirectsToNotProvisioned_AndNotifiesAdminOnce));
        var client = CreateClient(factory);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.SendAsync(CallbackRequest(
                "google", sub: "unknown-sub-1", challengedProvider: "google", email: "stranger@example.com"));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/login?error=oauth_not_provisioned", response.Headers.Location!.ToString());
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = await db.AdminNotifications.Where(n => n.Category == "auth").ToListAsync();
        var notification = Assert.Single(notifications);
        Assert.Contains("unknown-sub-1", notification.Message);
        Assert.Contains("stranger@example.com", notification.Message);

        // The rejected visitor holds no session.
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Callback_LinkedActiveUser_SignsInAndUpdatesLastLogin()
    {
        using var factory = BuildFactory(nameof(Callback_LinkedActiveUser_SignsInAndUpdatesLastLogin));
        var user = await SeedUserAsync(factory, "oauth-user", oauthProvider: "google", oauthSub: "sub-42");
        var client = CreateClient(factory);

        var response = await client.SendAsync(CallbackRequest("google", "sub-42", "google"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location!.ToString());

        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("oauth-user", doc.RootElement.GetProperty("username").GetString());

        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.NotNull(reloaded!.LastLoginAt);
    }

    [Fact]
    public async Task Callback_DisabledUser_IsRejectedWithoutSession()
    {
        using var factory = BuildFactory(nameof(Callback_DisabledUser_IsRejectedWithoutSession));
        await SeedUserAsync(factory, "disabled-user", AccountState.Disabled,
            oauthProvider: "google", oauthSub: "sub-disabled");
        var client = CreateClient(factory);

        var response = await client.SendAsync(CallbackRequest("google", "sub-disabled", "google"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=account_disabled", response.Headers.Location!.ToString());

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Callback_ProviderMismatch_IsRejected()
    {
        using var factory = BuildFactory(nameof(Callback_ProviderMismatch_IsRejected));
        await SeedUserAsync(factory, "mismatch-user", oauthProvider: "google", oauthSub: "sub-9");
        var client = CreateClient(factory);

        // Ticket was issued for a github challenge but replayed on the google callback.
        var response = await client.SendAsync(CallbackRequest("google", "sub-9", "github"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=oauth_failed", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_WithoutExternalTicket_IsRejected()
    {
        using var factory = BuildFactory(nameof(Callback_WithoutExternalTicket_IsRejected));
        var client = CreateClient(factory);

        var response = await client.GetAsync("/api/auth/oauth/google/callback");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=oauth_failed", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_UnconfiguredProvider_IsRejected()
    {
        using var factory = BuildFactory(nameof(Callback_UnconfiguredProvider_IsRejected));
        var client = CreateClient(factory);

        var response = await client.SendAsync(CallbackRequest("github", "sub-1", "github"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=oauth_failed", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ExternalIdentityHeaders_DoNotAuthorizeApiAccess()
    {
        using var factory = BuildFactory(nameof(ExternalIdentityHeaders_DoNotAuthorizeApiAccess));
        await SeedUserAsync(factory, "bypass-user", oauthProvider: "google", oauthSub: "sub-bypass");
        var client = CreateClient(factory);

        // An authenticated External-scheme identity must never satisfy bare
        // [Authorize]; only the main cookie issued by the callback may.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("X-Ext-Sub", "sub-bypass");
        request.Headers.Add("X-Ext-Provider", "google");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Account linking ----

    [Fact]
    public async Task Link_HappyPath_AttachesIdentityToSignedInUser()
    {
        using var factory = BuildFactory(nameof(Link_HappyPath_AttachesIdentityToSignedInUser));
        var user = await SeedUserAsync(factory, "linker");
        var client = CreateClient(factory);
        await LoginAsync(client, "linker");

        var response = await client.SendAsync(CallbackRequest(
            "google", "sub-link-1", "google", mode: "link", linkUser: user.Id.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard?oauth=linked", response.Headers.Location!.ToString());

        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.Equal("google", reloaded!.OAuthProvider);
        Assert.Equal("sub-link-1", reloaded.OAuthProviderUserId);
    }

    [Fact]
    public async Task Link_IdentityAlreadyUsedByAnotherUser_IsRejected()
    {
        using var factory = BuildFactory(nameof(Link_IdentityAlreadyUsedByAnotherUser_IsRejected));
        await SeedUserAsync(factory, "owner", oauthProvider: "google", oauthSub: "sub-owned");
        var user = await SeedUserAsync(factory, "second");
        var client = CreateClient(factory);
        await LoginAsync(client, "second");

        var response = await client.SendAsync(CallbackRequest(
            "google", "sub-owned", "google", mode: "link", linkUser: user.Id.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard?oauth=link_in_use", response.Headers.Location!.ToString());

        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.Null(reloaded!.OAuthProvider);
    }

    [Fact]
    public async Task Link_WithoutActiveSession_IsRejected()
    {
        using var factory = BuildFactory(nameof(Link_WithoutActiveSession_IsRejected));
        var user = await SeedUserAsync(factory, "no-session");
        var client = CreateClient(factory);

        var response = await client.SendAsync(CallbackRequest(
            "google", "sub-x", "google", mode: "link", linkUser: user.Id.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=oauth_failed", response.Headers.Location!.ToString());

        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.Null(reloaded!.OAuthProvider);
    }

    [Fact]
    public async Task Link_SessionUserMismatch_IsRejected()
    {
        using var factory = BuildFactory(nameof(Link_SessionUserMismatch_IsRejected));
        await SeedUserAsync(factory, "session-user");
        var victim = await SeedUserAsync(factory, "victim");
        var client = CreateClient(factory);
        await LoginAsync(client, "session-user");

        // The signed link state names a different user than the session holder.
        var response = await client.SendAsync(CallbackRequest(
            "google", "sub-y", "google", mode: "link", linkUser: victim.Id.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?error=oauth_failed", response.Headers.Location!.ToString());

        var reloaded = await ReloadUserAsync(factory, victim.Id);
        Assert.Null(reloaded!.OAuthProvider);
    }

    [Fact]
    public async Task Unlink_ClearsIdentity_WhenPasswordRemains()
    {
        using var factory = BuildFactory(nameof(Unlink_ClearsIdentity_WhenPasswordRemains));
        var user = await SeedUserAsync(factory, "unlinker", oauthProvider: "google", oauthSub: "sub-unlink");
        var client = CreateClient(factory);
        await LoginAsync(client, "unlinker");
        await AddCsrfTokenAsync(client);

        var response = await client.PostAsync("/api/auth/oauth/unlink", content: null);

        response.EnsureSuccessStatusCode();
        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.Null(reloaded!.OAuthProvider);
        Assert.Null(reloaded.OAuthProviderUserId);
    }

    [Fact]
    public async Task Unlink_WithoutPassword_IsBlocked()
    {
        using var factory = BuildFactory(nameof(Unlink_WithoutPassword_IsBlocked));
        var user = await SeedUserAsync(factory, "pwless",
            oauthProvider: "google", oauthSub: "sub-pwless", withPassword: false);
        var client = CreateClient(factory);

        // Establish the session via OAuth sign-in - the account has no password.
        var signIn = await client.SendAsync(CallbackRequest("google", "sub-pwless", "google"));
        Assert.Equal("/dashboard", signIn.Headers.Location!.ToString());
        await AddCsrfTokenAsync(client);

        var response = await client.PostAsync("/api/auth/oauth/unlink", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reloaded = await ReloadUserAsync(factory, user.Id);
        Assert.Equal("google", reloaded!.OAuthProvider);
    }
}
