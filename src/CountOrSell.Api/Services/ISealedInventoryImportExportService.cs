namespace CountOrSell.Api.Services;

public interface ISealedInventoryImportExportService
{
    /// <summary>Returns a blank CSV with the header row + example rows.</summary>
    (byte[] Data, string FileName) GenerateTemplate();

    /// <summary>Returns the user's sealed inventory as a CountOrSell CSV.</summary>
    Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Parses a CSV stream and bulk-inserts sealed inventory entries.</summary>
    Task<ImportResult> ImportAsync(Guid userId, Stream stream, CancellationToken ct = default);
}
