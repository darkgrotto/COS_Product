using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

// Taxonomy is published as metadata/taxonomy.json with a TaxonomyDto wrapper.
public class TaxonomyDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<SealedProductCategoryDto> Categories { get; set; } = new();
}

public class SealedProductCategoryDto
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }

    [JsonPropertyName("sub_types")]
    public List<SealedProductSubTypeDto> SubTypes { get; set; } = new();
}
