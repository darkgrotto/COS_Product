namespace CountOrSell.Domain.Models;

public class TopCardResult
{
    public string CardIdentifier { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal? MarketValue { get; set; }
}
