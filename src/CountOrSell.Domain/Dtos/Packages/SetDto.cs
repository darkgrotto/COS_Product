using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class SetDto
{
    [JsonPropertyName("set_code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("card_count")]
    public int TotalCards { get; set; }

    [JsonPropertyName("set_type")]
    public string? SetType { get; set; }

    // "yyyy-MM-dd" format or null
    [JsonPropertyName("released_at")]
    public string? ReleasedAt { get; set; }

    public DateOnly? ReleaseDate =>
        DateOnly.TryParseExact(ReleasedAt, "yyyy-MM-dd", out var d) ? d : null;
}
