using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class SealedProductDto
{
    [JsonPropertyName("product_id")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("set_code")]
    public string? SetCode { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category_slug")]
    public string? CategorySlug { get; set; }

    [JsonPropertyName("sub_type_slug")]
    public string? SubTypeSlug { get; set; }

    // Blob name for front image - used to build the image path in image store.
    [JsonPropertyName("front_image_blob_name")]
    public string? FrontImageBlobName { get; set; }

    // Blob name for supplemental image (optional).
    [JsonPropertyName("supplemental_image_blob_name")]
    public string? SupplementalImageBlobName { get; set; }
}
