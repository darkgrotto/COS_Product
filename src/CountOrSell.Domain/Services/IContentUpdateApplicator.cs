using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

public interface IContentUpdateApplicator
{
    // packageBaseUrl: the directory URL above manifest.json, used to fetch individual image blobs
    Task ApplyContentUpdateAsync(Stream packageStream, PackageManifest packageManifest, string packageBaseUrl, CancellationToken ct);
    Task ApplyImagesOnlyAsync(string packageBaseUrl, PackageManifest packageManifest, CancellationToken ct);
}
