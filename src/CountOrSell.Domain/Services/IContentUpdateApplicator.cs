using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

public interface IContentUpdateApplicator
{
    // packageBaseUrl: the directory URL above manifest.json, used to fetch individual image blobs
    // Returns how the image half went. Images are best-effort, so an incomplete result is
    // reported rather than thrown - callers are expected to surface it to an admin.
    Task<ImageSyncOutcome> ApplyContentUpdateAsync(Stream packageStream, PackageManifest packageManifest, string packageBaseUrl, CancellationToken ct);
    Task<ImageSyncOutcome> ApplyImagesOnlyAsync(string packageBaseUrl, PackageManifest packageManifest, CancellationToken ct);

    // Scoped variants used by targeted redownload.
    // scope: "all" | "cards-sets" | "sealed"
    Task ApplyMetadataOnlyAsync(Stream packageStream, PackageManifest packageManifest, string scope, CancellationToken ct);
    Task<ImageSyncOutcome> ApplyScopedImagesOnlyAsync(string packageBaseUrl, PackageManifest packageManifest, string scope, CancellationToken ct);
}
