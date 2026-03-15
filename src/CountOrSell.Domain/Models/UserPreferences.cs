namespace CountOrSell.Domain.Models;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public string? DefaultPage { get; set; }
    public bool SetCompletionRegularOnly { get; set; }
    // Navigation
    public User? User { get; set; }
}
