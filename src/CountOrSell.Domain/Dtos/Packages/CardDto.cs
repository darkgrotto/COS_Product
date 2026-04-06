using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class CardDto
{
    [JsonPropertyName("card_id")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("set_code")]
    public string SetCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    // Array of color symbols e.g. ["W", "U"]. Stored as comma-joined string.
    [JsonPropertyName("colors")]
    public List<string> Colors { get; set; } = new();

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("is_reserved")]
    public bool IsReserved { get; set; }

    [JsonPropertyName("oracle_ruling_uri")]
    public string? OracleRulingUri { get; set; }
}
