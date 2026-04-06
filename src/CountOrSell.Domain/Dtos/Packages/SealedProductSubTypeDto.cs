using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class SealedProductSubTypeDto
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }

    // CategorySlug is not present in the JSON - it is set from the parent category context
    // by ContentUpdateApplicator when flattening the taxonomy for DB upsert.
    public string CategorySlug { get; set; } = string.Empty;
}
