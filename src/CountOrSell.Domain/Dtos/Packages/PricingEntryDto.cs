using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Packages;

public class PricingEntryDto
{
    [JsonPropertyName("card_id")]
    public string CardIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("treatment")]
    public string TreatmentKey { get; set; } = string.Empty;

    [JsonPropertyName("price_usd")]
    public decimal? PriceUsd { get; set; }

    [JsonPropertyName("captured_at")]
    public DateTime CapturedAt { get; set; }
}
