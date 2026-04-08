namespace CountOrSell.Domain.Models;

// Canonical card data received via update packages.
// Stored as a flat view - no layer resolution in this project.
public class Card
{
    public string Identifier { get; set; } = string.Empty; // e.g. "eoe019"
    public string SetCode { get; set; } = string.Empty;    // e.g. "eoe"
    public string Name { get; set; } = string.Empty;
    public string? ManaCost { get; set; }
    public decimal? Cmc { get; set; }
    public string? Color { get; set; }          // comma-joined colors array, e.g. "W,U"
    public string? ColorIdentity { get; set; }  // comma-joined color_identity array
    public string? Keywords { get; set; }        // comma-joined keywords array
    public string? CardType { get; set; }
    public string? OracleText { get; set; }
    public string? Layout { get; set; }
    public string? OracleRulingUrl { get; set; }
    public decimal? CurrentMarketValue { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsReserved { get; set; }
    public string? Rarity { get; set; }
    public string? FlavorText { get; set; }
}
