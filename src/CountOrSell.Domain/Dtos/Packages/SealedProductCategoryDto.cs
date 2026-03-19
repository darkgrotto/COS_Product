namespace CountOrSell.Domain.Dtos.Packages;

public class SealedProductCategoryDto
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SealedProductSubTypeDto> SubTypes { get; set; } = new();
}
