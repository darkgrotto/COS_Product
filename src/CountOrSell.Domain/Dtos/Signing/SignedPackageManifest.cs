namespace CountOrSell.Domain.Dtos.Signing;

// Bundles the raw manifest bytes (as fetched from the URL, byte-identical to what the
// Backend signed), the parsed signature envelope, the deserialized PackageManifest, and the
// base URL the manifest was actually fetched from (the directory every per-file image fetch
// is appended to - carried here so callers never re-derive it from the untrusted manifest).
public sealed record SignedPackageManifest(
    byte[] ManifestBytes,
    SignedManifestEnvelope Envelope,
    PackageManifest Parsed,
    string BaseUrl);
