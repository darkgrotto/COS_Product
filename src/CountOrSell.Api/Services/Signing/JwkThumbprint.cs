using System.Security.Cryptography;
using System.Text;
using CountOrSell.Domain.Dtos.Signing;

namespace CountOrSell.Api.Services.Signing;

// Computes the RFC 7638 JWK thumbprint for an EC key.
// For EC keys the canonical JWK is the JSON object with members in lexicographic
// order: crv, kty, x, y. No whitespace, no trailing newline. SHA-256 of those
// UTF-8 bytes, then base64url-encoded without padding.
internal static class JwkThumbprint
{
    public static string Compute(CosJwk jwk) => Compute(jwk.Crv, jwk.Kty, jwk.X, jwk.Y);

    public static string Compute(string crv, string kty, string x, string y)
    {
        // Hand-built canonical JSON to guarantee byte-identical output across runtimes.
        // Member order is fixed (alphabetical); values are JWK fields which by
        // construction never contain characters that need JSON escaping.
        var canonical = $"{{\"crv\":\"{crv}\",\"kty\":\"{kty}\",\"x\":\"{x}\",\"y\":\"{y}\"}}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Base64Url.Encode(hash);
    }
}

internal static class Base64Url
{
    public static string Encode(byte[] data) =>
        Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public static byte[] Decode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
            case 0: break;
            default: throw new FormatException("Invalid base64url string length.");
        }
        return Convert.FromBase64String(padded);
    }
}
