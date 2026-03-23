using System.Security.Claims;
using CountOrSell.Api.Filters;
using CountOrSell.Api.Services;
using CountOrSell.Domain.Dtos.Requests;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CountOrSell.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;

    public InvitationsController(IInvitationService invitations)
    {
        _invitations = invitations;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string BaseUrl =>
        $"{Request.Scheme}://{Request.Host}";

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [DemoLocked]
    public async Task<IActionResult> Create([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required." });

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return BadRequest(new { error = "Role must be Admin or GeneralUser." });

        var (invitation, inviteUrl) = await _invitations.CreateInvitationAsync(
            request.Email.Trim(), role, CurrentUserId, BaseUrl, ct);

        return Ok(new
        {
            invitation.Id,
            invitation.Email,
            invitation.Role,
            invitation.ExpiresAt,
            InviteUrl = inviteUrl
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var pending = await _invitations.GetPendingInvitationsAsync(ct);
        return Ok(pending.Select(i => new
        {
            i.Id,
            i.Email,
            Role = i.Role.ToString(),
            i.CreatedAt,
            i.ExpiresAt
        }));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [DemoLocked]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var found = await _invitations.RevokeInvitationAsync(id, ct);
        if (!found) return NotFound();
        return NoContent();
    }

    [HttpGet("{token}/validate")]
    public async Task<IActionResult> Validate(string token, CancellationToken ct)
    {
        var invitation = await _invitations.GetValidInvitationByTokenAsync(token, ct);
        if (invitation is null)
            return NotFound(new { error = "Invitation is invalid or has expired." });

        return Ok(new
        {
            invitation.Email,
            Role = invitation.Role.ToString(),
            invitation.ExpiresAt
        });
    }

    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, [FromBody] AcceptInvitationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { error = "Username is required." });

        var result = await _invitations.AcceptInvitationAsync(token, request.Username.Trim(), request.Password, ct);
        if (!result.Success)
            return Conflict(new { error = result.Error });

        return Ok();
    }
}
