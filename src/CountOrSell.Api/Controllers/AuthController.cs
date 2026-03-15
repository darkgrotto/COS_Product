using System.Security.Claims;
using CountOrSell.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ILocalAuthService _localAuth;
    private readonly IOAuthConfigService _oauthConfig;

    public AuthController(ILocalAuthService localAuth, IOAuthConfigService oauthConfig)
    {
        _localAuth = localAuth;
        _oauthConfig = oauthConfig;
    }

    [HttpPost("login")]
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

        return Ok(new { userId = user.Id, username = user.Username, role = user.Role.ToString() });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("oauth/{provider}")]
    public IActionResult OAuthLogin(string provider)
    {
        if (!_oauthConfig.IsConfigured(provider))
            return BadRequest(new { error = $"OAuth provider '{provider}' is not configured on this instance." });

        var redirectUrl = Url.Action(nameof(OAuthCallback), new { provider });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> OAuthCallback(string provider, CancellationToken ct)
    {
        if (!_oauthConfig.IsConfigured(provider))
            return BadRequest(new { error = $"OAuth provider '{provider}' is not configured on this instance." });

        // OAuth callback handling - full implementation in auth step
        await Task.CompletedTask;
        return Ok();
    }
}

public record LoginRequest(string Username, string Password);
