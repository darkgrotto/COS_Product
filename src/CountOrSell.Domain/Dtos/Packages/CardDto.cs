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

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("cmc")]
    public decimal? Cmc { get; set; }

    // Array of color symbols e.g. ["W", "U"]. Stored as comma-joined string.
    [JsonPropertyName("colors")]
    public List<string> Colors { get; set; } = new();

    // Array of color identity symbols. Stored as comma-joined string.
    [JsonPropertyName("color_identity")]
    public List<string> ColorIdentity { get; set; } = new();

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; set; }

    [JsonPropertyName("layout")]
    public string? Layout { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("is_reserved")]
    public bool IsReserved { get; set; }

    [JsonPropertyName("oracle_ruling_uri")]
    public string? OracleRulingUri { get; set; }

    // Valid treatments for this card - array of normalized_name strings.
    [JsonPropertyName("treatments")]
    public List<string> Treatments { get; set; } = new();
}
