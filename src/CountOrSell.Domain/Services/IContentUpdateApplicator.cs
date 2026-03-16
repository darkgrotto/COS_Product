namespace CountOrSell.Domain.Services;

public interface IContentUpdateApplicator
{
    Task ApplyContentUpdateAsync(Stream packageStream, string contentVersion, CancellationToken ct);
}
