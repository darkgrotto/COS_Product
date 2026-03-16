namespace CountOrSell.Domain.Models;

// In-app notification for Product instance admins.
// Categories: "update", "backup", "schema"
public class AdminNotification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "update", "backup", "schema"
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
