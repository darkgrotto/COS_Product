namespace CountOrSell.Domain.Dtos.Packages;

public class SealedProductDto
{
    public string Identifier { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategorySlug { get; set; }
    public string? SubTypeSlug { get; set; }
    public decimal? CurrentMarketValue { get; set; }
}
