namespace CountOrSell.Domain.Dtos.Packages;

public class CardDto
{
    public string Identifier { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? CardType { get; set; }
    public decimal? MarketValue { get; set; }
    public bool IsReserved { get; set; }
}
