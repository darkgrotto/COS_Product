namespace CountOrSell.Domain.Dtos.Packages;

public class SealedProductSubTypeDto
{
    public string Slug { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
