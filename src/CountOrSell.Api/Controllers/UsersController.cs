using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserRepository _users;

    public UsersController(IUserService userService, IUserRepository users)
    {
        _userService = userService;
        _users = users;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Username,
            u.DisplayName,
            Role      = u.Role.ToString(),
            State     = u.State.ToString(),
            AuthType  = u.AuthType.ToString(),
            u.IsBuiltinAdmin,
            u.CreatedAt,
            u.LastLoginAt
        }));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Username, user.DisplayName });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateLocalUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { error = "Username is required." });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Password is required." });

        var role = request.Role == "Admin"
            ? CountOrSell.Domain.Models.Enums.UserRole.Admin
            : CountOrSell.Domain.Models.Enums.UserRole.GeneralUser;

        var result = await _userService.CreateLocalUserAsync(
            request.Username.Trim(),
            string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
            request.Password,
            role,
            ct);

        if (!result.Success)
            return Conflict(new { error = result.Error });

        return Ok();
    }

    [HttpPost("{id}/disable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        var result = await _userService.DisableUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/remove")]
    [Authorize(Roles = "Admin")]
    [DemoLocked]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var result = await _userService.RemoveUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/demote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Demote(Guid id, CancellationToken ct)
    {
        var result = await _userService.DemoteUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/promote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Promote(Guid id, CancellationToken ct)
    {
        var result = await _userService.PromoteUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/reenable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReEnable(Guid id, CancellationToken ct)
    {
        var result = await _userService.ReEnableUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpGet("{id}/exports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetExports(Guid id, CancellationToken ct)
    {
        var files = await _userService.GetExportFilesAsync(id, ct);
        return Ok(files.Select(f => new
        {
            f.Id, f.UserId, f.Username, f.RemovedAt,
            f.FileSizeBytes, f.CreatedAt
        }));
    }

    [HttpGet("{id}/exports/{exportId}/download")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadExport(Guid id, Guid exportId, CancellationToken ct)
    {
        var exportFile = await _userService.GetExportFileAsync(exportId, ct);
        if (exportFile == null || exportFile.UserId != id) return NotFound();
        if (!System.IO.File.Exists(exportFile.FilePath))
            return NotFound(new { error = "Export file not found on disk." });

        var bytes = await System.IO.File.ReadAllBytesAsync(exportFile.FilePath, ct);
        var fileName = Path.GetFileName(exportFile.FilePath);
        return File(bytes, "application/json", fileName);
    }

    [HttpDelete("{id}/exports/{exportId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteExport(Guid id, Guid exportId, CancellationToken ct)
    {
        await _userService.DeleteExportFileAsync(exportId, ct);
        return NoContent();
    }

    [HttpGet("me/preferences")]
    [Authorize]
    public async Task<IActionResult> GetMyPreferences(CancellationToken ct)
    {
        var prefs = await _userService.GetPreferencesAsync(CurrentUserId, ct);
        if (prefs == null)
            return Ok(new { setCompletionRegularOnly = false, defaultPage = (string?)null });

        return Ok(new
        {
            prefs.SetCompletionRegularOnly,
            prefs.DefaultPage
        });
    }

    [HttpPatch("me/preferences")]
    [Authorize]
    public async Task<IActionResult> PatchMyPreferences([FromBody] UserPreferencesRequest request, CancellationToken ct)
    {
        await _userService.UpdatePreferencesAsync(CurrentUserId, request.SetCompletionRegularOnly, request.DefaultPage, ct);
        return Ok();
    }
}

public sealed record CreateLocalUserRequest(
    string Username,
    string? DisplayName,
    string Password,
    string Role);
