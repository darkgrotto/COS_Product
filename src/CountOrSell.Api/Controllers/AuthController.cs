using System.Security.Claims;
using CountOrSell.Api.Auth;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ILocalAuthService _localAuth;
    private readonly IOAuthConfigService _oauthConfig;
    private readonly IUserRepository _users;

    public AuthController(ILocalAuthService localAuth, IOAuthConfigService oauthConfig, IUserRepository users)
    {
        _localAuth = localAuth;
        _oauthConfig = oauthConfig;
        _users = users;
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isBuiltinAdmin = User.FindFirstValue("is_builtin_admin");
        return Ok(new
        {
            userId,
            username,
            role,
            isBuiltinAdmin = bool.Parse(isBuiltinAdmin ?? "false"),
        });
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
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
