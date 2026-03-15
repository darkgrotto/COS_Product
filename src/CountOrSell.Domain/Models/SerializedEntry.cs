using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Models;

public class SerializedEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CardIdentifier { get; set; } = string.Empty;
    public string TreatmentKey { get; set; } = string.Empty;
    public int SerialNumber { get; set; }
    public int PrintRunTotal { get; set; }
    public CardCondition Condition { get; set; }
    public bool Autographed { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public User? User { get; set; }
    public Treatment? Treatment { get; set; }
}
