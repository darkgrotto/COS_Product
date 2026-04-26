using CountOrSell.Domain.Dtos.Signing;

namespace CountOrSell.Domain.Services;

public enum SignatureVerificationStatus
{
    Valid,
    UnsupportedAlgorithm,
    JwksUnavailable,
    KidNotFound,
    KeyMaterialInvalid,
    SignatureFormatInvalid,
    SignatureMismatch,
}

public sealed record SignatureVerificationResult(
    SignatureVerificationStatus Status,
    string Message)
{
    public bool IsValid => Status == SignatureVerificationStatus.Valid;
    public static SignatureVerificationResult Ok() =>
        new(SignatureVerificationStatus.Valid, "Signature verified.");
}

public interface IManifestSignatureVerifier
{
    // Verifies a detached ES256 signature over the canonical manifest bytes.
    // Returns Valid only when:
    //   - envelope.Alg == "ES256"
    //   - the JWKS contains a key whose kid matches envelope.Kid
    //   - that key's public material reconstructs to a valid P-256 point
    //   - envelope.Sig is a 64-byte raw P1363 ECDSA signature
    //   - ECDSA-SHA256 verification against manifestBytes succeeds
    Task<SignatureVerificationResult> VerifyAsync(
        byte[] manifestBytes,
        SignedManifestEnvelope envelope,
        CancellationToken ct);
}
