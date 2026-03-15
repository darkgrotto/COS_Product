using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public UserRole Role { get; set; }
    public bool IsBuiltinAdmin { get; set; }
    public AccountState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    // OAuth-only fields
    public string? OAuthProvider { get; set; }
    public string? OAuthProviderUserId { get; set; }
    // Local-only fields
    public string? PasswordHash { get; set; }
    // User preferences (navigation)
    public UserPreferences? Preferences { get; set; }
}
