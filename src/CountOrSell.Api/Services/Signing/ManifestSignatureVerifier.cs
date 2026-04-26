using System.Security.Cryptography;
using CountOrSell.Domain.Dtos.Signing;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Signing;

// Verifies an ES256 / P-256 detached signature over the raw bytes of a per-package manifest.
// The signature format is IEEE P1363 (raw r||s, 64 bytes for P-256), NOT DER.
public sealed class ManifestSignatureVerifier : IManifestSignatureVerifier
{
    private const string ExpectedAlg = "ES256";
    private const string ExpectedKty = "EC";
    private const string ExpectedCrv = "P-256";
    private const int P256CoordLength = 32;
    private const int P1363SignatureLength = 64;

    private readonly IJwksProvider _jwks;
    private readonly ILogger<ManifestSignatureVerifier> _logger;

    public ManifestSignatureVerifier(IJwksProvider jwks, ILogger<ManifestSignatureVerifier> logger)
    {
        _jwks = jwks;
        _logger = logger;
    }

    public async Task<SignatureVerificationResult> VerifyAsync(
        byte[] manifestBytes,
        SignedManifestEnvelope envelope,
        CancellationToken ct)
    {
        if (!string.Equals(envelope.Alg, ExpectedAlg, StringComparison.Ordinal))
            return new SignatureVerificationResult(
                SignatureVerificationStatus.UnsupportedAlgorithm,
                $"Unsupported signature algorithm '{envelope.Alg}'. Expected '{ExpectedAlg}'.");

        if (string.IsNullOrEmpty(envelope.Kid))
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KidNotFound,
                "Signature envelope is missing kid.");

        var jwk = await _jwks.GetKeyByKidAsync(envelope.Kid, ct);
        if (jwk == null)
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KidNotFound,
                $"Signing key '{envelope.Kid}' is not in the trusted JWKS.");

        if (!string.Equals(jwk.Kty, ExpectedKty, StringComparison.Ordinal)
            || !string.Equals(jwk.Crv, ExpectedCrv, StringComparison.Ordinal))
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KeyMaterialInvalid,
                $"Signing key '{envelope.Kid}' is not an EC P-256 key.");

        byte[] xBytes, yBytes;
        try
        {
            xBytes = LeftPad(Base64Url.Decode(jwk.X), P256CoordLength);
            yBytes = LeftPad(Base64Url.Decode(jwk.Y), P256CoordLength);
        }
        catch (FormatException)
        {
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KeyMaterialInvalid,
                $"Signing key '{envelope.Kid}' has malformed EC coordinates.");
        }

        if (xBytes.Length != P256CoordLength || yBytes.Length != P256CoordLength)
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KeyMaterialInvalid,
                $"Signing key '{envelope.Kid}' has incorrectly-sized EC coordinates.");

        byte[] sigBytes;
        try
        {
            sigBytes = Convert.FromBase64String(envelope.Sig);
        }
        catch (FormatException)
        {
            return new SignatureVerificationResult(
                SignatureVerificationStatus.SignatureFormatInvalid,
                "Signature is not valid base64.");
        }

        if (sigBytes.Length != P1363SignatureLength)
            return new SignatureVerificationResult(
                SignatureVerificationStatus.SignatureFormatInvalid,
                $"Signature is {sigBytes.Length} bytes; expected {P1363SignatureLength} (raw P1363 r||s).");

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportParameters(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = xBytes, Y = yBytes }
            });

            var ok = ecdsa.VerifyData(
                manifestBytes,
                sigBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            if (!ok)
            {
                _logger.LogWarning(
                    "Manifest signature verification failed for kid {Kid} ({ManifestBytes} bytes)",
                    envelope.Kid, manifestBytes.Length);
                return new SignatureVerificationResult(
                    SignatureVerificationStatus.SignatureMismatch,
                    "Signature does not match manifest contents.");
            }

            return SignatureVerificationResult.Ok();
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Cryptographic failure during signature verification for kid {Kid}", envelope.Kid);
            return new SignatureVerificationResult(
                SignatureVerificationStatus.KeyMaterialInvalid,
                "Signing key could not be imported.");
        }
    }

    // Pads a byte array on the left with zeros to the requested length. EC coordinates
    // can be shorter than the curve's coordinate size when the leading bytes are zero.
    private static byte[] LeftPad(byte[] input, int length)
    {
        if (input.Length == length) return input;
        if (input.Length > length) return input;
        var padded = new byte[length];
        Buffer.BlockCopy(input, 0, padded, length - input.Length, input.Length);
        return padded;
    }
}
