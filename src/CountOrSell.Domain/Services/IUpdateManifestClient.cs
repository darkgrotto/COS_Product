using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Signing;

namespace CountOrSell.Domain.Services;

public interface IUpdateManifestClient
{
    Task<UpdateManifest?> FetchManifestAsync(CancellationToken ct);

    // Fetches manifest.json AND manifest.json.sig from the same directory and returns
    // them together so a caller can run signature verification against the byte-identical
    // manifest body. Returns null if either fetch or parse fails.
    Task<SignedPackageManifest?> FetchSignedPackageManifestAsync(string manifestUrl, CancellationToken ct);
}
