using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

public interface IUpdateManifestClient
{
    Task<UpdateManifest?> FetchManifestAsync(CancellationToken ct);
}
