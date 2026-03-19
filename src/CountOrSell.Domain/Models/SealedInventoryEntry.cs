using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Models;

public class SealedInventoryEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ProductIdentifier { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public CardCondition Condition { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public string? Notes { get; set; }
    public string? CategorySlug { get; set; }
    public string? SubTypeSlug { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public User? User { get; set; }
}
