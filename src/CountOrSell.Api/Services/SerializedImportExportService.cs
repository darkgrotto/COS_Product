using System.Globalization;
using System.Text;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public sealed class SerializedImportExportService : ISerializedImportExportService
{
    private readonly AppDbContext _db;
    private readonly ISerializedRepository _serialized;
    private readonly ITreatmentValidator _treatments;

    public SerializedImportExportService(
        AppDbContext db,
        ISerializedRepository serialized,
        ITreatmentValidator treatments)
    {
        _db = db;
        _serialized = serialized;
        _treatments = treatments;
    }

    private const string Header =
        "CardIdentifier,Treatment,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes";

    public (byte[] Data, string FileName) GenerateTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine("# All columns are required except Notes. SerialNumber and PrintRunTotal must be positive integers.");
        sb.AppendLine("# Condition: NM, LP, MP, HP, DMG. Autographed: true or false. AcquisitionDate: YYYY-MM-DD.");
        sb.AppendLine("# Remove or replace this example row before importing.");
        sb.AppendLine("EOE019,serialized,42,100,NM,false,2026-01-15,49.99,From local trade");
        return (Encoding.UTF8.GetBytes(sb.ToString()), "serialized-template.csv");
    }

    public async Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _serialized.GetByUserAsync(userId, ct);
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var e in entries)
        {
            sb.Append(ImportCsvHelpers.Escape(e.CardIdentifier.ToUpperInvariant())); sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.TreatmentKey));                       sb.Append(',');
            sb.Append(e.SerialNumber.ToString(CultureInfo.InvariantCulture));         sb.Append(',');
            sb.Append(e.PrintRunTotal.ToString(CultureInfo.InvariantCulture));        sb.Append(',');
            sb.Append(ImportCsvHelpers.ConditionDisplay(e.Condition));                sb.Append(',');
            sb.Append(e.Autographed ? "true" : "false");                              sb.Append(',');
            sb.Append(e.AcquisitionDate.ToString("yyyy-MM-dd"));                       sb.Append(',');
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.AppendLine(ImportCsvHelpers.Escape(e.Notes));
        }
        return (Encoding.UTF8.GetBytes(sb.ToString()), $"serialized-{DateTime.UtcNow:yyyy-MM-dd}.csv");
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
        var toAdd = new List<SerializedEntry>();

        for (int i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowIdx = i + 2;
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            var firstCell = row.Count > 0 ? row[0].TrimStart() : string.Empty;
            if (firstCell.StartsWith('#')) continue;

            var rawId        = ImportCsvHelpers.Col(row, header, "cardidentifier");
            var rawTreatment = ImportCsvHelpers.Col(row, header, "treatment");
            var rawSerial    = ImportCsvHelpers.Col(row, header, "serialnumber");
            var rawPrintRun  = ImportCsvHelpers.Col(row, header, "printruntotal");
            var rawCondition = ImportCsvHelpers.Col(row, header, "condition");
            var rawAuto      = ImportCsvHelpers.Col(row, header, "autographed");
            var rawDate      = ImportCsvHelpers.Col(row, header, "acquisitiondate");
            var rawPrice     = ImportCsvHelpers.Col(row, header, "acquisitionprice");
            var notes        = ImportCsvHelpers.Col(row, header, "notes");

            if (string.IsNullOrWhiteSpace(rawId))
            { failures.Add($"Row {rowIdx}: CardIdentifier is required."); continue; }

            var cardId = rawId.Trim().ToLowerInvariant();
            if (!CardIdentifierValidator.IsValid(cardId))
            { failures.Add($"Row {rowIdx}: invalid card identifier '{rawId.ToUpperInvariant()}'."); continue; }

            var treatment = string.IsNullOrWhiteSpace(rawTreatment) ? "regular" : rawTreatment.Trim().ToLowerInvariant();
            if (!validTreatments.Contains(treatment))
            { failures.Add($"Row {rowIdx}: unknown treatment '{treatment}'."); continue; }

            if (!ImportCsvHelpers.TryParseInt(rawSerial, out var serialNumber) || serialNumber < 1)
            { failures.Add($"Row {rowIdx}: SerialNumber must be a positive integer."); continue; }

            if (!ImportCsvHelpers.TryParseInt(rawPrintRun, out var printRunTotal) || printRunTotal < 1)
            { failures.Add($"Row {rowIdx}: PrintRunTotal must be a positive integer."); continue; }

            if (serialNumber > printRunTotal)
            { failures.Add($"Row {rowIdx}: SerialNumber ({serialNumber}) cannot exceed PrintRunTotal ({printRunTotal})."); continue; }

            if (!ImportCsvHelpers.TryParseCondition(rawCondition, out var condition))
            { failures.Add($"Row {rowIdx}: invalid Condition '{rawCondition}'. Expected NM, LP, MP, HP, or DMG."); continue; }

            if (!ImportCsvHelpers.TryParseDate(rawDate, out var acquisitionDate))
            { failures.Add($"Row {rowIdx}: AcquisitionDate is required (YYYY-MM-DD)."); continue; }

            if (!ImportCsvHelpers.TryParsePrice(rawPrice, out var acquisitionPrice) || acquisitionPrice < 0m)
            { failures.Add($"Row {rowIdx}: AcquisitionPrice must be a non-negative number."); continue; }

            toAdd.Add(new SerializedEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardIdentifier = cardId,
                TreatmentKey = treatment,
                SerialNumber = serialNumber,
                PrintRunTotal = printRunTotal,
                Condition = condition,
                Autographed = ImportCsvHelpers.ParseBool(rawAuto),
                AcquisitionDate = acquisitionDate,
                AcquisitionPrice = acquisitionPrice,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        if (toAdd.Count == 0)
            return new ImportResult(0, 0, failures.Count, failures);

        var requestedIds = toAdd.Select(e => e.CardIdentifier).Distinct().ToList();
        var existingCards = await _db.Cards
            .Where(c => requestedIds.Contains(c.Identifier))
            .Select(c => c.Identifier)
            .ToListAsync(ct);
        var existingCardSet = existingCards.ToHashSet();

        int added = 0;
        foreach (var e in toAdd)
        {
            if (!existingCardSet.Contains(e.CardIdentifier))
            {
                failures.Add($"Card not found in database: {e.CardIdentifier.ToUpperInvariant()}");
                continue;
            }
            await _serialized.CreateAsync(e, ct);
            added++;
        }

        return new ImportResult(added, 0, failures.Count, failures);
    }
}
