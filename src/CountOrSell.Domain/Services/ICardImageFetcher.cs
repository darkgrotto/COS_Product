namespace CountOrSell.Domain.Services;

public interface ICardImageFetcher
{
    /// <summary>
    /// Attempts to fetch a card image from a third-party source using the card identifier.
    /// Returns the image bytes, or null if the image could not be fetched.
    /// </summary>
    Task<byte[]?> FetchAsync(string cardIdentifier, CancellationToken ct);
}
