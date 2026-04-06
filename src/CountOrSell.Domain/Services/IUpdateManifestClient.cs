using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

public interface IUpdateManifestClient
{
    Task<UpdateManifest?> FetchManifestAsync(CancellationToken ct);
    Task<PackageManifest?> FetchPackageManifestAsync(string manifestUrl, CancellationToken ct);
}
