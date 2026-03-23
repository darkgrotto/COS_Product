using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Api.Services;

public interface IInvitationService
{
    Task<(UserInvitation Invitation, string InviteUrl)> CreateInvitationAsync(
        string email, UserRole role, Guid createdByUserId, string baseUrl, CancellationToken ct = default);
    Task<List<UserInvitation>> GetPendingInvitationsAsync(CancellationToken ct = default);
    Task<bool> RevokeInvitationAsync(Guid id, CancellationToken ct = default);
    Task<UserInvitation?> GetValidInvitationByTokenAsync(string token, CancellationToken ct = default);
    Task<UserServiceResult> AcceptInvitationAsync(
        string token, string username, string password, CancellationToken ct = default);
}
