using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

public interface IContentUpdateApplicator
{
    Task ApplyContentUpdateAsync(Stream packageStream, PackageManifest packageManifest, CancellationToken ct);
}
