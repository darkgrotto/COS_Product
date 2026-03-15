namespace CountOrSell.Domain.Models;

public class WishlistEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CardIdentifier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // Navigation
    public User? User { get; set; }
}
