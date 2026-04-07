namespace CountOrSell.Api.Services;

public interface IAvatarService
{
    /// <summary>
    /// Validates the uploaded file (content type + magic bytes) and resizes/re-encodes
    /// it to a JPEG of at most 200x200 pixels. Returns null with an error message on failure.
    /// </summary>
    Task<(byte[]? jpeg, string? error)> ProcessAvatarAsync(Stream input, string contentType, CancellationToken ct);

    Task<byte[]?> GetAvatarAsync(Guid userId, CancellationToken ct);
    Task SaveAvatarAsync(Guid userId, byte[] jpeg, CancellationToken ct);
    Task DeleteAvatarAsync(Guid userId, CancellationToken ct);
    Task<bool> HasAvatarAsync(Guid userId, CancellationToken ct);
}
