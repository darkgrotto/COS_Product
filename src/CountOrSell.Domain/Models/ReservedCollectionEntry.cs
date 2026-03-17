namespace CountOrSell.Domain.Models;

// Projection used by the reserved list collection endpoint.
// Joins a user's collection entries with canonical card data where the card is on the Reserved List.
public class ReservedCollectionEntry
{
    public Guid EntryId { get; set; }
    public string CardIdentifier { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public string Treatment { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public bool Autographed { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public decimal? MarketValue { get; set; }
}
