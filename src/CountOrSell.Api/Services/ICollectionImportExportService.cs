using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Services;

public enum CollectionExportFormat
{
    Cos,
    Moxfield,
    Deckbox,
    TcgPlayer,
    DragonShield,
    Manabox,
}

public record ImportResult(
    int Added,
    int Skipped,
    int Failed,
    IReadOnlyList<string> Failures
);

public interface ICollectionImportExportService
{
    /// <summary>Returns a CSV byte array for the user's collection in the requested format.</summary>
    Task<(byte[] Data, string FileName)> ExportAsync(
        Guid userId,
        CollectionExportFormat format,
        CancellationToken ct = default);

    /// <summary>Returns a filtered CSV byte array for the user's collection in the requested format.</summary>
    Task<(byte[] Data, string FileName)> ExportFilteredAsync(
        Guid userId,
        CollectionExportFormat format,
        CollectionFilter filter,
        CancellationToken ct = default);

    /// <summary>Parses a CSV stream and bulk-inserts matching collection entries.</summary>
    Task<ImportResult> ImportAsync(
        Guid userId,
        CollectionExportFormat format,
        Stream stream,
        CancellationToken ct = default);
}
