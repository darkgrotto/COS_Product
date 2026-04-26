namespace CountOrSell.Domain.Dtos.Signing;

// Bundles the raw manifest bytes (as fetched from the URL, byte-identical to what the
// Backend signed), the parsed signature envelope, and the deserialized PackageManifest.
public sealed record SignedPackageManifest(
    byte[] ManifestBytes,
    SignedManifestEnvelope Envelope,
    PackageManifest Parsed);
