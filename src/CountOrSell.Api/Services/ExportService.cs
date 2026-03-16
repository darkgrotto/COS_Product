using System.Text.Json;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class ExportService : IExportService
{
    private readonly ICollectionRepository _collection;
    private readonly ISerializedRepository _serialized;
    private readonly ISlabRepository _slabs;
    private readonly ISealedInventoryRepository _sealedInventory;
    private readonly IWishlistRepository _wishlist;
    private readonly IUserRepository _users;
    private readonly IUserExportFileRepository _exportFiles;
    private readonly string _exportDirectory;

    public ExportService(
        ICollectionRepository collection,
        ISerializedRepository serialized,
        ISlabRepository slabs,
        ISealedInventoryRepository sealedInventory,
        IWishlistRepository wishlist,
        IUserRepository users,
        IUserExportFileRepository exportFiles,
        IConfiguration configuration)
    {
        _collection = collection;
        _serialized = serialized;
        _slabs = slabs;
        _sealedInventory = sealedInventory;
        _wishlist = wishlist;
        _users = users;
        _exportFiles = exportFiles;
        _exportDirectory = configuration["ExportDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "exports");
    }

    public async Task<UserExportFile> ExportUserDataAsync(Guid userId, string username, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var collectionEntries = await _collection.GetByUserAsync(userId, ct);
        var serializedEntries = await _serialized.GetByUserAsync(userId, ct);
        var slabEntries = await _slabs.GetByUserAsync(userId, ct);
        var sealedEntries = await _sealedInventory.GetByUserAsync(userId, ct);
        var wishlistEntries = await _wishlist.GetByUserAsync(userId, ct);

        var exportData = new
        {
            ExportVersion = 1,
            ExportedAt = DateTime.UtcNow,
            User = new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.AuthType,
                user.Role,
                user.CreatedAt,
                Preferences = user.Preferences == null ? null : new
                {
                    user.Preferences.DefaultPage,
                    user.Preferences.SetCompletionRegularOnly
                }
            },
            CollectionEntries = collectionEntries.Select(e => new
            {
                e.Id, e.CardIdentifier, e.TreatmentKey, e.Quantity,
                Condition = e.Condition.ToString(),
                e.Autographed, e.AcquisitionDate, e.AcquisitionPrice,
                e.Notes, e.CreatedAt, e.UpdatedAt
            }),
            SerializedEntries = serializedEntries.Select(e => new
            {
                e.Id, e.CardIdentifier, e.TreatmentKey,
                e.SerialNumber, e.PrintRunTotal,
                Condition = e.Condition.ToString(),
                e.Autographed, e.AcquisitionDate, e.AcquisitionPrice,
                e.Notes, e.CreatedAt, e.UpdatedAt
            }),
            SlabEntries = slabEntries.Select(e => new
            {
                e.Id, e.CardIdentifier, e.TreatmentKey,
                e.GradingAgencyCode, e.Grade, e.CertificateNumber,
                e.SerialNumber, e.PrintRunTotal,
                Condition = e.Condition.ToString(),
                e.Autographed, e.AcquisitionDate, e.AcquisitionPrice,
                e.Notes, e.CreatedAt, e.UpdatedAt
            }),
            SealedInventoryEntries = sealedEntries.Select(e => new
            {
                e.Id, e.ProductIdentifier, e.Quantity,
                Condition = e.Condition.ToString(),
                e.AcquisitionDate, e.AcquisitionPrice,
                e.Notes, e.CreatedAt, e.UpdatedAt
            }),
            WishlistEntries = wishlistEntries.Select(e => new
            {
                e.Id, e.CardIdentifier, e.CreatedAt
            })
        };

        Directory.CreateDirectory(_exportDirectory);

        var safeUsername = string.Concat(username.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = $"export_{safeUsername}_{timestamp}.json";
        var filePath = Path.Combine(_exportDirectory, fileName);

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, ct);

        var fileInfo = new FileInfo(filePath);

        var exportFile = new UserExportFile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            RemovedAt = DateTime.UtcNow,
            FilePath = filePath,
            FileSizeBytes = fileInfo.Length,
            CreatedAt = DateTime.UtcNow
        };

        await _exportFiles.CreateAsync(exportFile, ct);
        return exportFile;
    }

    public Task<List<UserExportFile>> GetExportFilesForUserAsync(Guid userId, CancellationToken ct = default) =>
        _exportFiles.GetByUserIdAsync(userId, ct);

    public async Task DeleteExportFileAsync(Guid exportFileId, CancellationToken ct = default)
    {
        var exportFile = await _exportFiles.GetByIdAsync(exportFileId, ct);
        if (exportFile == null) return;

        if (File.Exists(exportFile.FilePath))
            File.Delete(exportFile.FilePath);

        await _exportFiles.DeleteAsync(exportFileId, ct);
    }
}
