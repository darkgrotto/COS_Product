namespace CountOrSell.Domain.Models;

/// <summary>
/// Represents a card from canonical data received via update packages.
/// Card identifiers follow pattern ^[a-z0-9]{3,4}[0-9]{3,4}$ (stored lowercase).
/// </summary>
public class Card
{
    /// <summary>
    /// Canonical card identifier (e.g. "eoe019"), stored lowercase.
    /// Pattern: ^[a-z0-9]{3,4}[0-9]{3,4}$
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Set code portion of the card identifier (e.g. "eoe"), stored lowercase.
    /// Pattern: ^[a-z0-9]{3,4}$
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// Card name as received from update packages.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Collector number within the set (zero-padded to 3 digits for 001-999,
    /// unpadded 4-digit for 1000-9999).
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current market value as received from the most recent update package
    /// or TCGPlayer direct query.
    /// </summary>
    public decimal? MarketValue { get; set; }

    /// <summary>
    /// Date of the last card data update (not per-card, applies to the whole set).
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}
