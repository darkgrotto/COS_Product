using System.Security.Claims;
using CountOrSell.Api.Auth;
using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Keys carried through the DataProtection-signed OAuth state so the
    // callback can trust them (the browser cannot tamper with Items).
    private const string StateProvider = "cos_provider";
    private const string StateMode = "cos_mode";
    private const string StateLinkUser = "cos_link_user";
    private const string ModeLink = "link";

    private readonly ILocalAuthService _localAuth;
    private readonly IOAuthConfigService _oauthConfig;
    private readonly IUserRepository _users;
    private readonly IAvatarService _avatars;
    private readonly IAdminNotificationService _adminNotifications;

    public AuthController(
        ILocalAuthService localAuth,
        IOAuthConfigService oauthConfig,
        IUserRepository users,
        IAvatarService avatars,
        IAdminNotificationService adminNotifications)
    {
        _localAuth = localAuth;
        _oauthConfig = oauthConfig;
        _users = users;
        _avatars = avatars;
        _adminNotifications = adminNotifications;
    }

    // Issues a CSRF token to the SPA. The browser keeps the matching cookie token
    // (HttpOnly, SameSite=Strict); the SPA echoes the body value back on every
    // state-changing request via the X-CSRF-TOKEN header.
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isBuiltinAdmin = User.FindFirstValue("is_builtin_admin");

        string? displayName = null;
        bool hasAvatar = false;
        string? authType = null;
        string? oauthProvider = null;
        bool canUnlinkOAuth = false;
        if (Guid.TryParse(userId, out var uid))
        {
            var user = await _users.GetByIdAsync(uid, ct);
            displayName = user?.DisplayName;
            hasAvatar = await _avatars.HasAvatarAsync(uid, ct);
            authType = user?.AuthType.ToString();
            oauthProvider = user?.OAuthProvider;
            // Unlinking must never leave the account without a sign-in method.
            canUnlinkOAuth = user?.OAuthProvider is not null && user.PasswordHash is not null;
        }

        return Ok(new
        {
            userId,
            username,
            displayName,
            role,
            isBuiltinAdmin = bool.Parse(isBuiltinAdmin ?? "false"),
            hasAvatar,
            authType,
            oauthProvider,
            canUnlinkOAuth,
        });
    }

    // Login is the bootstrap call: there is no session yet, so we cannot require an
    // anti-forgery token here. Cookie SameSite=Strict still prevents cross-site forced
    // login from a hostile origin.
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _localAuth.ValidateCredentialsAsync(request.Username, request.Password, ct);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("is_builtin_admin", user.IsBuiltinAdmin.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        var hasAvatar = await _avatars.HasAvatarAsync(user.Id, ct);
        return Ok(new
        {
            userId = user.Id,
            username = user.Username,
            displayName = user.DisplayName,
            role = user.Role.ToString(),
            isBuiltinAdmin = user.IsBuiltinAdmin,
            hasAvatar,
            authType = user.AuthType.ToString(),
            oauthProvider = user.OAuthProvider,
            canUnlinkOAuth = user.OAuthProvider is not null && user.PasswordHash is not null,
        });
    }

    [HttpPatch("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (request.NewPassword.Length < 15)
            return BadRequest(new { error = "New password must be at least 15 characters." });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();
        if (user.AuthType != AuthType.Local)
            return BadRequest(new { error = "Password change is not available for OAuth accounts." });
        if (user.PasswordHash is null || !_localAuth.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Current password is incorrect." });

        user.PasswordHash = _localAuth.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    // Lists the OAuth providers an anonymous visitor can sign in with. The login
    // page uses this to decide which buttons to render; it exposes nothing beyond
    // what starting a sign-in attempt would reveal anyway.
    [HttpGet("oauth/providers")]
    [AllowAnonymous]
    public IActionResult OAuthProviderList()
    {
        var providers = OAuthProviders.All
            .Where(_oauthConfig.IsConfigured)
            .Select(id => new { id, displayName = OAuthProviders.DisplayName(id) });
        return Ok(providers);
    }

    [HttpGet("oauth/{provider}")]
    public IActionResult OAuthLogin(string provider)
    {
        provider = provider.ToLowerInvariant();
        if (!OAuthProviders.TryGetScheme(provider, out var scheme) || !_oauthConfig.IsConfigured(provider))
            return BadRequest(new { error = $"OAuth provider '{provider}' is not configured on this instance." });

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { provider }),
        };
        properties.Items[StateProvider] = provider;
        return Challenge(properties, scheme);
    }

    // Starts the flow that attaches an OAuth identity to the signed-in user's
    // account. The user id travels in the signed OAuth state, and the callback
    // re-checks that the same user still holds the session.
    [HttpGet("oauth/{provider}/link")]
    [Authorize]
    public IActionResult OAuthLink(string provider)
    {
        provider = provider.ToLowerInvariant();
        if (!OAuthProviders.TryGetScheme(provider, out var scheme) || !_oauthConfig.IsConfigured(provider))
            return BadRequest(new { error = $"OAuth provider '{provider}' is not configured on this instance." });

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { provider }),
        };
        properties.Items[StateProvider] = provider;
        properties.Items[StateMode] = ModeLink;
        properties.Items[StateLinkUser] = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Challenge(properties, scheme);
    }

    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> OAuthCallback(string provider, CancellationToken ct)
    {
        provider = provider.ToLowerInvariant();
        if (!OAuthProviders.TryGetScheme(provider, out _) || !_oauthConfig.IsConfigured(provider))
            return Redirect("/login?error=oauth_failed");

        var external = await HttpContext.AuthenticateAsync(OAuthProviders.ExternalScheme);

        // The external ticket is single-use: consumed here regardless of outcome.
        await HttpContext.SignOutAsync(OAuthProviders.ExternalScheme);

        if (!external.Succeeded || external.Principal is null || external.Properties is null)
            return Redirect("/login?error=oauth_failed");

        // The ticket must have been issued by the provider this callback claims,
        // otherwise a Google identity could be replayed against the GitHub route
        // and stored under the wrong provider key.
        external.Properties.Items.TryGetValue(StateProvider, out var challengedProvider);
        if (!string.Equals(challengedProvider, provider, StringComparison.Ordinal))
            return Redirect("/login?error=oauth_failed");

        var providerUserId = external.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(providerUserId))
            return Redirect("/login?error=oauth_failed");

        external.Properties.Items.TryGetValue(StateMode, out var mode);
        if (string.Equals(mode, ModeLink, StringComparison.Ordinal))
            return await CompleteLinkAsync(provider, providerUserId, external.Properties, ct);

        var user = await _users.GetByOAuthAsync(provider, providerUserId, ct);
        if (user is null)
        {
            await NotifyUnknownOAuthSignInAsync(provider, providerUserId, external.Principal, ct);
            return Redirect("/login?error=oauth_not_provisioned");
        }

        if (user.State != AccountState.Active)
            return Redirect("/login?error=account_disabled");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("is_builtin_admin", user.IsBuiltinAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return Redirect("/dashboard");
    }

    // Detaches the OAuth identity from the signed-in user's account. Blocked
    // when the account has no password, so it cannot orphan the sign-in.
    [HttpPost("oauth/unlink")]
    [Authorize]
    public async Task<IActionResult> OAuthUnlink(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();
        if (user.OAuthProvider is null)
            return BadRequest(new { error = "No OAuth account is linked." });
        if (user.PasswordHash is null)
            return BadRequest(new { error = "Cannot unlink: this account has no password and would lose its only sign-in method." });

        user.OAuthProvider = null;
        user.OAuthProviderUserId = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        return Ok();
    }

    private async Task<IActionResult> CompleteLinkAsync(
        string provider, string providerUserId, AuthenticationProperties externalProperties, CancellationToken ct)
    {
        externalProperties.Items.TryGetValue(StateLinkUser, out var linkUserRaw);
        if (!Guid.TryParse(linkUserRaw, out var linkUserId))
            return Redirect("/dashboard?oauth=link_failed");

        // The link only completes for the browser session of the user who
        // initiated it: the main cookie must still authenticate as that user.
        var session = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var sessionUserId = session.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!session.Succeeded || sessionUserId != linkUserId.ToString())
            return Redirect("/login?error=oauth_failed");

        var user = await _users.GetByIdAsync(linkUserId, ct);
        if (user is null || user.State != AccountState.Active)
            return Redirect("/login?error=oauth_failed");

        var existing = await _users.GetByOAuthAsync(provider, providerUserId, ct);
        if (existing is not null && existing.Id != user.Id)
            return Redirect("/dashboard?oauth=link_in_use");

        if (user.OAuthProvider is not null
            && !(user.OAuthProvider == provider && user.OAuthProviderUserId == providerUserId))
            return Redirect("/dashboard?oauth=link_already_linked");

        user.OAuthProvider = provider;
        user.OAuthProviderUserId = providerUserId;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return Redirect("/dashboard?oauth=linked");
    }

    private Task NotifyUnknownOAuthSignInAsync(
        string provider, string providerUserId, ClaimsPrincipal externalPrincipal, CancellationToken ct)
    {
        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
        var name = externalPrincipal.FindFirstValue(ClaimTypes.Name);

        var who = (email, name) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{name} <{email}>",
            ({ Length: > 0 }, _) => email!,
            (_, { Length: > 0 }) => name!,
            _ => "an unidentified account",
        };

        var message =
            $"Sign-in attempt by an unrecognized {OAuthProviders.DisplayName(provider)} account: {who} " +
            $"(subject id {providerUserId}). To grant access, create or invite a user and have them link " +
            $"this {OAuthProviders.DisplayName(provider)} account from their profile.";
        return _adminNotifications.NotifyOnceAsync(message, "auth", ct);
    }
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
