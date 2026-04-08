namespace CountOrSell.Domain.Models;

// Per-treatment market price for a card, received via pricing.json in update packages.
public class CardPrice
{
    public string CardIdentifier { get; set; } = string.Empty;
    public string TreatmentKey { get; set; } = string.Empty;
    public decimal? PriceUsd { get; set; }
    public DateTime CapturedAt { get; set; }
}
