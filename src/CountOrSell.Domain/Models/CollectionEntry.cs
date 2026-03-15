namespace CountOrSell.Domain.Models;

/// <summary>
/// A single collection entry representing one or more copies of a card
/// with specific treatment, condition, and acquisition details.
/// </summary>
public class CollectionEntry
{
    public int Id { get; set; }

    /// <summary>
    /// The owning user's ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Card identifier (e.g. "eoe019"), stored lowercase.
    /// Pattern: ^[a-z0-9]{3,4}[0-9]{3,4}$
    /// </summary>
    public string CardId { get; set; } = string.Empty;

    /// <summary>
    /// Treatment key from the treatments reference table (e.g. "foil", "surge-foil").
    /// Never hardcoded - always references the treatments table.
    /// </summary>
    public string Treatment { get; set; } = string.Empty;

    /// <summary>
    /// Number of copies in this collection entry.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Condition of the cards in this entry.
    /// </summary>
    public CardCondition Condition { get; set; }

    /// <summary>
    /// Whether the card(s) are autographed.
    /// Condition is preserved and displayed alongside the autograph indicator.
    /// </summary>
    public bool Autographed { get; set; }

    /// <summary>
    /// Date the cards were acquired. Defaults to current date at time of entry.
    /// </summary>
    public DateOnly AcquisitionDate { get; set; }

    /// <summary>
    /// Price paid per card at time of acquisition.
    /// Defaults to current market value at time of entry.
    /// </summary>
    public decimal AcquisitionPrice { get; set; }

    /// <summary>
    /// Optional free-text notes.
    /// </summary>
    public string? Notes { get; set; }
}
