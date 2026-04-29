namespace CountOrSell.Api.Services.Signing;

// RFC 7638 thumbprints of every signing key the Product is willing to trust.
// Baked into the binary so a compromised website cannot cause the Product to
// adopt a fresh attacker-controlled key (TOFU defense).
//
// Compute with:
//   curl -s https://www.countorsell.com/.well-known/cos-pubkey.json \
//     | jq -c '.keys[0] | {crv, kty, x, y}' \
//     | tr -d '\n' \
//     | openssl dgst -sha256 -binary \
//     | openssl base64 -A \
//     | tr '+/' '-_' \
//     | tr -d '='
//
// When rotating, append the predecessor's thumbprint here BEFORE shipping the
// release that publishes the new key, so existing instances keep verifying.
public static class TrustedKeyThumbprints
{
    public static readonly IReadOnlyList<string> Values = new[]
    {
        // Initial COS_Backend signing key (kid f9127444b43b4205810139114a0b1a6b, created 2026-04-26 UTC).
        "xlUO-ThKSDDri1Mc0znW_cZx883ZUc4AWRKibJhue6Q",
    };
}
