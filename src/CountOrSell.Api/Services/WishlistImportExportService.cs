using System.Text;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public sealed class WishlistImportExportService : IWishlistImportExportService
{
    private readonly AppDbContext _db;
    private readonly IWishlistRepository _wishlist;
    private readonly ITreatmentValidator _treatments;

    public WishlistImportExportService(
        AppDbContext db,
        IWishlistRepository wishlist,
        ITreatmentValidator treatments)
    {
        _db = db;
        _wishlist = wishlist;
        _treatments = treatments;
    }

    private const string Header = "CardIdentifier,Treatment";

    public (byte[] Data, string FileName) GenerateTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine("# Required columns: CardIdentifier (e.g. EOE019), Treatment (e.g. regular, foil).");
        sb.AppendLine("# Remove or replace this example row before importing.");
        sb.AppendLine("EOE019,regular");
        return (Encoding.UTF8.GetBytes(sb.ToString()), "wishlist-template.csv");
    }

    public async Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _wishlist.GetByUserAsync(userId, ct);
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var e in entries)
        {
            sb.Append(ImportCsvHelpers.Escape(e.CardIdentifier.ToUpperInvariant()));
            sb.Append(',');
            sb.AppendLine(ImportCsvHelpers.Escape(e.TreatmentKey));
        }
        return (Encoding.UTF8.GetBytes(sb.ToString()), $"wishlist-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    public async Task<ImportResult> ImportAsync(Guid userId, Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var allRows = ImportCsvHelpers.ParseCsv(reader);
        if (allRows.Count < 2)
            return new ImportResult(0, 0, 0, []);

        var header = allRows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        var dataRows = allRows.Skip(1).ToList();

        var validTreatments = await _treatments.GetValidKeysAsync(ct);
        var failures = new List<string>();
        var toAdd = new List<WishlistEntry>();
        var seenInBatch = new HashSet<(string, string)>();

        for (int i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowIdx = i + 2;
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            var firstCell = row.Count > 0 ? row[0].TrimStart() : string.Empty;
            if (firstCell.StartsWith('#')) continue;

            var rawId = ImportCsvHelpers.Col(row, header, "cardidentifier");
            var rawTreatment = ImportCsvHelpers.Col(row, header, "treatment");

            if (string.IsNullOrWhiteSpace(rawId))
            {
                failures.Add($"Row {rowIdx}: CardIdentifier is required.");
                continue;
            }

            var cardId = rawId.Trim().ToLowerInvariant();
            if (!CardIdentifierValidator.IsValid(cardId))
            {
                failures.Add($"Row {rowIdx}: invalid card identifier '{rawId.ToUpperInvariant()}'.");
                continue;
            }

            var treatment = string.IsNullOrWhiteSpace(rawTreatment)
                ? "regular"
                : rawTreatment.Trim().ToLowerInvariant();

            if (!validTreatments.Contains(treatment))
            {
                failures.Add($"Row {rowIdx}: unknown treatment '{treatment}'.");
                continue;
            }

            if (!seenInBatch.Add((cardId, treatment)))
            {
                failures.Add($"Row {rowIdx}: duplicate row for {cardId.ToUpperInvariant()} ({treatment}) within file.");
                continue;
            }

            toAdd.Add(new WishlistEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardIdentifier = cardId,
                TreatmentKey = treatment,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (toAdd.Count == 0)
            return new ImportResult(0, 0, failures.Count, failures);

        // Verify cards exist in canonical data.
        var requestedIds = toAdd.Select(e => e.CardIdentifier).Distinct().ToList();
        var existingCards = await _db.Cards
            .Where(c => requestedIds.Contains(c.Identifier))
            .Select(c => c.Identifier)
            .ToListAsync(ct);
        var existingCardSet = existingCards.ToHashSet();

        // Drop the user's already-existing (CardIdentifier, TreatmentKey) pairs.
        var existingPairs = await _db.WishlistEntries
            .Where(w => w.UserId == userId && requestedIds.Contains(w.CardIdentifier))
            .Select(w => new { w.CardIdentifier, w.TreatmentKey })
            .ToListAsync(ct);
        var existingPairSet = existingPairs
            .Select(p => (p.CardIdentifier, p.TreatmentKey))
            .ToHashSet();

        int skipped = 0;
        var finalToAdd = new List<WishlistEntry>();
        foreach (var e in toAdd)
        {
            if (!existingCardSet.Contains(e.CardIdentifier))
            {
                failures.Add($"Card not found in database: {e.CardIdentifier.ToUpperInvariant()}");
                continue;
            }
            if (existingPairSet.Contains((e.CardIdentifier, e.TreatmentKey)))
            {
                skipped++;
                continue;
            }
            finalToAdd.Add(e);
        }

        foreach (var entry in finalToAdd)
            await _wishlist.CreateAsync(entry, ct);

        return new ImportResult(finalToAdd.Count, skipped, failures.Count, failures);
    }
}
