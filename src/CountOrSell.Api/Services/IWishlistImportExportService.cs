namespace CountOrSell.Api.Services;

public interface IWishlistImportExportService
{
    /// <summary>Returns a blank CSV with the header row + one commented example row.</summary>
    (byte[] Data, string FileName) GenerateTemplate();

    /// <summary>Returns the user's wishlist as a CountOrSell CSV.</summary>
    Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Parses a CSV stream and bulk-inserts wishlist entries the user does not already have.</summary>
    Task<ImportResult> ImportAsync(Guid userId, Stream stream, CancellationToken ct = default);
}
