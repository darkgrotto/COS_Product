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

    public SetupController(IUserRepository users, ILocalAuthService localAuth)
    {
        _users = users;
        _localAuth = localAuth;
    }

    /// <summary>
    /// Called once by the first-run wizard after deployment to create the initial accounts.
    /// Returns 409 if any users already exist.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        [FromBody] SetupInitializeRequest request,
        CancellationToken ct)
    {
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
}

public sealed record SetupInitializeRequest(
    string AdminUsername,
    string AdminPassword,
    string GeneralUserUsername,
    string GeneralUserPassword);
