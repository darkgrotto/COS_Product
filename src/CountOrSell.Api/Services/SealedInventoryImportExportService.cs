using System.Globalization;
using System.Text;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public sealed class SealedInventoryImportExportService : ISealedInventoryImportExportService
{
    private readonly AppDbContext _db;
    private readonly ISealedInventoryRepository _inventory;
    private readonly ISealedTaxonomyRepository _taxonomy;

    public SealedInventoryImportExportService(
        AppDbContext db,
        ISealedInventoryRepository inventory,
        ISealedTaxonomyRepository taxonomy)
    {
        _db = db;
        _inventory = inventory;
        _taxonomy = taxonomy;
    }

    private const string Header =
        "ProductIdentifier,Quantity,Condition,AcquisitionDate,AcquisitionPrice,CategorySlug,SubTypeSlug,Notes";

    public (byte[] Data, string FileName) GenerateTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine("# Required: ProductIdentifier, Quantity, Condition, AcquisitionDate, AcquisitionPrice.");
        sb.AppendLine("# CategorySlug and SubTypeSlug are optional but must match taxonomy entries when provided.");
        sb.AppendLine("# SubTypeSlug requires CategorySlug. Notes is optional.");
        sb.AppendLine("# Remove or replace this example row before importing.");
        sb.AppendLine("eoe-collector-booster-box,1,NM,2026-01-15,249.99,booster,collector,Sealed factory case");
        return (Encoding.UTF8.GetBytes(sb.ToString()), "sealed-inventory-template.csv");
    }

    public async Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _inventory.GetByUserAsync(userId, ct);
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var e in entries)
        {
            sb.Append(ImportCsvHelpers.Escape(e.ProductIdentifier));                  sb.Append(',');
            sb.Append(e.Quantity.ToString(CultureInfo.InvariantCulture));             sb.Append(',');
            sb.Append(ImportCsvHelpers.ConditionDisplay(e.Condition));                sb.Append(',');
            sb.Append(e.AcquisitionDate.ToString("yyyy-MM-dd"));                       sb.Append(',');
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.CategorySlug));                       sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.SubTypeSlug));                        sb.Append(',');
            sb.AppendLine(ImportCsvHelpers.Escape(e.Notes));
        }
        return (Encoding.UTF8.GetBytes(sb.ToString()), $"sealed-inventory-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    public async Task<ImportResult> ImportAsync(Guid userId, Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var allRows = ImportCsvHelpers.ParseCsv(reader);
        if (allRows.Count < 2)
            return new ImportResult(0, 0, 0, []);

        var header = allRows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        var dataRows = allRows.Skip(1).ToList();

        var categories = await _taxonomy.GetAllCategoriesAsync(ct);
        var subTypes = await _taxonomy.GetAllSubTypesAsync(ct);
        var categorySet = categories.Select(c => c.Slug).ToHashSet();
        var subTypesByCategory = subTypes
            .GroupBy(s => s.CategorySlug)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Slug).ToHashSet());

        var failures = new List<string>();
        var toAdd = new List<SealedInventoryEntry>();

        for (int i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowIdx = i + 2;
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            var firstCell = row.Count > 0 ? row[0].TrimStart() : string.Empty;
            if (firstCell.StartsWith('#')) continue;

            var rawProductId = ImportCsvHelpers.Col(row, header, "productidentifier");
            var rawQty       = ImportCsvHelpers.Col(row, header, "quantity");
            var rawCondition = ImportCsvHelpers.Col(row, header, "condition");
            var rawDate      = ImportCsvHelpers.Col(row, header, "acquisitiondate");
            var rawPrice     = ImportCsvHelpers.Col(row, header, "acquisitionprice");
            var rawCategory  = ImportCsvHelpers.Col(row, header, "categoryslug");
            var rawSubType   = ImportCsvHelpers.Col(row, header, "subtypeslug");
            var notes        = ImportCsvHelpers.Col(row, header, "notes");

            if (string.IsNullOrWhiteSpace(rawProductId))
            { failures.Add($"Row {rowIdx}: ProductIdentifier is required."); continue; }
            var productId = rawProductId.Trim();

            if (!ImportCsvHelpers.TryParseInt(rawQty, out var quantity) || quantity < 1)
            { failures.Add($"Row {rowIdx}: Quantity must be a positive integer."); continue; }

            if (!ImportCsvHelpers.TryParseCondition(rawCondition, out var condition))
            { failures.Add($"Row {rowIdx}: invalid Condition '{rawCondition}'. Expected NM, LP, MP, HP, or DMG."); continue; }

            if (!ImportCsvHelpers.TryParseDate(rawDate, out var acquisitionDate))
            { failures.Add($"Row {rowIdx}: AcquisitionDate is required (YYYY-MM-DD)."); continue; }

            if (!ImportCsvHelpers.TryParsePrice(rawPrice, out var acquisitionPrice) || acquisitionPrice < 0m)
            { failures.Add($"Row {rowIdx}: AcquisitionPrice must be a non-negative number."); continue; }

            string? categorySlug = null;
            string? subTypeSlug = null;
            if (!string.IsNullOrWhiteSpace(rawCategory))
            {
                categorySlug = rawCategory.Trim().ToLowerInvariant();
                if (!categorySet.Contains(categorySlug))
                { failures.Add($"Row {rowIdx}: unknown CategorySlug '{categorySlug}'."); continue; }
            }
            if (!string.IsNullOrWhiteSpace(rawSubType))
            {
                if (categorySlug == null)
                { failures.Add($"Row {rowIdx}: SubTypeSlug requires CategorySlug."); continue; }
                subTypeSlug = rawSubType.Trim().ToLowerInvariant();
                if (!subTypesByCategory.TryGetValue(categorySlug, out var subs) || !subs.Contains(subTypeSlug))
                { failures.Add($"Row {rowIdx}: SubTypeSlug '{subTypeSlug}' is not valid for category '{categorySlug}'."); continue; }
            }

            toAdd.Add(new SealedInventoryEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductIdentifier = productId,
                Quantity = quantity,
                Condition = condition,
                AcquisitionDate = acquisitionDate,
                AcquisitionPrice = acquisitionPrice,
                CategorySlug = categorySlug,
                SubTypeSlug = subTypeSlug,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        if (toAdd.Count == 0)
            return new ImportResult(0, 0, failures.Count, failures);

        var requestedProductIds = toAdd.Select(e => e.ProductIdentifier).Distinct().ToList();
        var existingProducts = await _db.SealedProducts
            .Where(p => requestedProductIds.Contains(p.Identifier))
            .Select(p => p.Identifier)
            .ToListAsync(ct);
        var existingProductSet = existingProducts.ToHashSet();

        int added = 0;
        foreach (var e in toAdd)
        {
            if (!existingProductSet.Contains(e.ProductIdentifier))
            {
                failures.Add($"Sealed product not found in database: {e.ProductIdentifier}");
                continue;
            }
            await _inventory.CreateAsync(e, ct);
            added++;
        }

        return new ImportResult(added, 0, failures.Count, failures);
    }
}
