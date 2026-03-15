using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserRepository _users;

    public UsersController(IUserService userService, IUserRepository users)
    {
        _userService = userService;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users.Select(u => new
        {
            u.Id, u.Username, u.DisplayName, u.Role, u.State,
            u.AuthType, u.IsBuiltinAdmin, u.CreatedAt, u.LastLoginAt
        }));
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        var result = await _userService.DisableUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/remove")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var result = await _userService.RemoveUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/demote")]
    public async Task<IActionResult> Demote(Guid id, CancellationToken ct)
    {
        var result = await _userService.DemoteUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/promote")]
    public async Task<IActionResult> Promote(Guid id, CancellationToken ct)
    {
        var result = await _userService.PromoteUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id}/reenable")]
    public async Task<IActionResult> ReEnable(Guid id, CancellationToken ct)
    {
        var result = await _userService.ReEnableUserAsync(id, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });
        return Ok();
    }
}
