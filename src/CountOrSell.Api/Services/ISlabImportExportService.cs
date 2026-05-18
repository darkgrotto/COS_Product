namespace CountOrSell.Api.Services;

public interface ISlabImportExportService
{
    /// <summary>Returns a blank CSV with the header row + example rows.</summary>
    (byte[] Data, string FileName) GenerateTemplate();

    /// <summary>Returns the user's slabs as a CountOrSell CSV.</summary>
    Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Parses a CSV stream and bulk-inserts slab entries.</summary>
    Task<ImportResult> ImportAsync(Guid userId, Stream stream, CancellationToken ct = default);
}
