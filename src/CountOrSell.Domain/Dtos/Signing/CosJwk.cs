using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Signing;

// JWK shape served at https://www.countorsell.com/.well-known/cos-pubkey.json.
// Only EC / P-256 / ES256 keys are supported.
public sealed class CosJwk
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; } = string.Empty;

    [JsonPropertyName("crv")]
    public string Crv { get; set; } = string.Empty;

    [JsonPropertyName("alg")]
    public string Alg { get; set; } = string.Empty;

    [JsonPropertyName("use")]
    public string Use { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    // Base64url-encoded EC X coordinate.
    [JsonPropertyName("x")]
    public string X { get; set; } = string.Empty;

    // Base64url-encoded EC Y coordinate.
    [JsonPropertyName("y")]
    public string Y { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CosJwks
{
    [JsonPropertyName("keys")]
    public List<CosJwk> Keys { get; set; } = new();
}
