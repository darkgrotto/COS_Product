using CountOrSell.Api.Auth;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ILocalAuthService _localAuth;
    private readonly IConfiguration _config;

    public SetupController(IUserRepository users, ILocalAuthService localAuth, IConfiguration config)
    {
        _users = users;
        _localAuth = localAuth;
        _config = config;
    }

    /// <summary>
    /// Called once by the first-run wizard after deployment to create the initial accounts.
    /// Requires a SETUP_TOKEN environment variable to be set; the wizard sends the matching
    /// value in the request body. Returns 404 when no token is configured, 401 on mismatch,
    /// and 409 if any users already exist.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        [FromBody] SetupInitializeRequest request,
        CancellationToken ct)
    {
        var configuredToken = _config["SETUP_TOKEN"];
        if (string.IsNullOrEmpty(configuredToken))
            return NotFound();

        if (!CryptographicEquals(request.SetupToken, configuredToken))
            return Unauthorized(new { error = "Invalid setup token." });

        if (await _users.AnyAsync(ct))
            return Conflict(new { error = "Already initialized." });

        if (string.IsNullOrWhiteSpace(request.AdminUsername))
            return BadRequest(new { error = "Admin username is required." });
        if (string.IsNullOrWhiteSpace(request.AdminPassword) || request.AdminPassword.Length < 15)
            return BadRequest(new { error = "Admin password must be at least 15 characters." });
        if (string.IsNullOrWhiteSpace(request.GeneralUserUsername))
            return BadRequest(new { error = "General user username is required." });
        if (string.IsNullOrWhiteSpace(request.GeneralUserPassword) || request.GeneralUserPassword.Length < 15)
            return BadRequest(new { error = "General user password must be at least 15 characters." });
        if (request.AdminUsername.Equals(request.GeneralUserUsername, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Admin and general user must have different usernames." });

        var now = DateTime.UtcNow;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = request.AdminUsername,
            DisplayName = request.AdminUsername,
            AuthType = AuthType.Local,
            Role = UserRole.Admin,
            IsBuiltinAdmin = true,
            State = AccountState.Active,
            PasswordHash = _localAuth.HashPassword(request.AdminPassword),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var generalUser = new User
        {
            Id = Guid.NewGuid(),
            Username = request.GeneralUserUsername,
            DisplayName = request.GeneralUserUsername,
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            PasswordHash = _localAuth.HashPassword(request.GeneralUserPassword),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _users.CreateAsync(admin, ct);
        await _users.CreateAsync(generalUser, ct);

        return Ok(new { message = "Initialized." });
    }

    // Constant-time string comparison to prevent timing attacks on the token.
    private static bool CryptographicEquals(string? a, string b)
    {
        if (a is null) return false;
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b));
    }
}

public sealed record SetupInitializeRequest(
    string SetupToken,
    string AdminUsername,
    string AdminPassword,
    string GeneralUserUsername,
    string GeneralUserPassword);
