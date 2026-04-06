using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class TreatmentDto
{
    // treatment_id is present in the JSON but not persisted to the DB (normalized_name is the PK).
    [JsonPropertyName("treatment_id")]
    public int TreatmentId { get; set; }

    [JsonPropertyName("normalized_name")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}
