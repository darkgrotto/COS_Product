using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos.Signing;

// Detached signature envelope for a per-package manifest.json.
// Fetched from <manifest_url>.sig and from the manifest.json.sig entry inside the package ZIP.
public sealed class SignedManifestEnvelope
{
    [JsonPropertyName("alg")]
    public string Alg { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    // Raw IEEE P1363 signature concatenation of r and s (64 bytes for P-256), base64-encoded.
    // NOT DER-encoded.
    [JsonPropertyName("sig")]
    public string Sig { get; set; } = string.Empty;
}
