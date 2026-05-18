using System.Globalization;
using System.Text;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public sealed class SlabImportExportService : ISlabImportExportService
{
    private readonly AppDbContext _db;
    private readonly ISlabRepository _slabs;
    private readonly IGradingAgencyRepository _agencies;
    private readonly ITreatmentValidator _treatments;

    public SlabImportExportService(
        AppDbContext db,
        ISlabRepository slabs,
        IGradingAgencyRepository agencies,
        ITreatmentValidator treatments)
    {
        _db = db;
        _slabs = slabs;
        _agencies = agencies;
        _treatments = treatments;
    }

    private const string Header =
        "CardIdentifier,Treatment,GradingAgency,Grade,CertificateNumber,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes";

    public (byte[] Data, string FileName) GenerateTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine("# Required: CardIdentifier, Treatment, GradingAgency (e.g. PSA, BGS, CGC), Grade, CertificateNumber,");
        sb.AppendLine("# Condition, AcquisitionDate, AcquisitionPrice. SerialNumber and PrintRunTotal are paired:");
        sb.AppendLine("# provide both or neither. Autographed defaults to false. Notes is optional.");
        sb.AppendLine("# Remove or replace this example row before importing.");
        sb.AppendLine("EOE019,regular,PSA,9.5,12345678,,,NM,false,2026-01-15,89.99,Pop 12");
        return (Encoding.UTF8.GetBytes(sb.ToString()), "slabs-template.csv");
    }

    public async Task<(byte[] Data, string FileName)> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _slabs.GetByUserAsync(userId, ct);
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var e in entries)
        {
            sb.Append(ImportCsvHelpers.Escape(e.CardIdentifier.ToUpperInvariant())); sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.TreatmentKey));                       sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.GradingAgencyCode.ToUpperInvariant())); sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.Grade));                              sb.Append(',');
            sb.Append(ImportCsvHelpers.Escape(e.CertificateNumber));                  sb.Append(',');
            sb.Append(e.SerialNumber?.ToString(CultureInfo.InvariantCulture) ?? "");  sb.Append(',');
            sb.Append(e.PrintRunTotal?.ToString(CultureInfo.InvariantCulture) ?? ""); sb.Append(',');
            sb.Append(ImportCsvHelpers.ConditionDisplay(e.Condition));                sb.Append(',');
            sb.Append(e.Autographed ? "true" : "false");                              sb.Append(',');
            sb.Append(e.AcquisitionDate.ToString("yyyy-MM-dd"));                       sb.Append(',');
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.AppendLine(ImportCsvHelpers.Escape(e.Notes));
        }
        return (Encoding.UTF8.GetBytes(sb.ToString()), $"slabs-{DateTime.UtcNow:yyyy-MM-dd}.csv");
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
        var allAgencies = await _agencies.GetAllAsync(ct);
        var validAgencyCodes = allAgencies
            .Where(a => a.Active)
            .Select(a => a.Code.ToLowerInvariant())
            .ToHashSet();

        var failures = new List<string>();
        var toAdd = new List<SlabEntry>();
        var seenCertsInBatch = new HashSet<(string, string)>(); // (agency, cert)

        for (int i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowIdx = i + 2;
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            var firstCell = row.Count > 0 ? row[0].TrimStart() : string.Empty;
            if (firstCell.StartsWith('#')) continue;

            var rawId        = ImportCsvHelpers.Col(row, header, "cardidentifier");
            var rawTreatment = ImportCsvHelpers.Col(row, header, "treatment");
            var rawAgency    = ImportCsvHelpers.Col(row, header, "gradingagency");
            var rawGrade     = ImportCsvHelpers.Col(row, header, "grade");
            var rawCert      = ImportCsvHelpers.Col(row, header, "certificatenumber");
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

            if (string.IsNullOrWhiteSpace(rawAgency))
            { failures.Add($"Row {rowIdx}: GradingAgency is required."); continue; }
            var agencyCode = rawAgency.Trim().ToLowerInvariant();
            if (!validAgencyCodes.Contains(agencyCode))
            { failures.Add($"Row {rowIdx}: unknown GradingAgency '{rawAgency.ToUpperInvariant()}'."); continue; }

            if (string.IsNullOrWhiteSpace(rawGrade))
            { failures.Add($"Row {rowIdx}: Grade is required."); continue; }

            if (string.IsNullOrWhiteSpace(rawCert))
            { failures.Add($"Row {rowIdx}: CertificateNumber is required."); continue; }
            var certNumber = rawCert.Trim();

            int? serialNumber = null;
            int? printRunTotal = null;
            bool hasSerial = !string.IsNullOrWhiteSpace(rawSerial);
            bool hasPrintRun = !string.IsNullOrWhiteSpace(rawPrintRun);
            if (hasSerial != hasPrintRun)
            { failures.Add($"Row {rowIdx}: SerialNumber and PrintRunTotal must be provided together."); continue; }
            if (hasSerial)
            {
                if (!ImportCsvHelpers.TryParseInt(rawSerial, out var sn) || sn < 1)
                { failures.Add($"Row {rowIdx}: SerialNumber must be a positive integer."); continue; }
                if (!ImportCsvHelpers.TryParseInt(rawPrintRun, out var pr) || pr < 1)
                { failures.Add($"Row {rowIdx}: PrintRunTotal must be a positive integer."); continue; }
                if (sn > pr)
                { failures.Add($"Row {rowIdx}: SerialNumber ({sn}) cannot exceed PrintRunTotal ({pr})."); continue; }
                serialNumber = sn;
                printRunTotal = pr;
            }

            if (!ImportCsvHelpers.TryParseCondition(rawCondition, out var condition))
            { failures.Add($"Row {rowIdx}: invalid Condition '{rawCondition}'. Expected NM, LP, MP, HP, or DMG."); continue; }

            if (!ImportCsvHelpers.TryParseDate(rawDate, out var acquisitionDate))
            { failures.Add($"Row {rowIdx}: AcquisitionDate is required (YYYY-MM-DD)."); continue; }

            if (!ImportCsvHelpers.TryParsePrice(rawPrice, out var acquisitionPrice) || acquisitionPrice < 0m)
            { failures.Add($"Row {rowIdx}: AcquisitionPrice must be a non-negative number."); continue; }

            if (!seenCertsInBatch.Add((agencyCode, certNumber)))
            {
                failures.Add($"Row {rowIdx}: duplicate certificate {rawAgency.ToUpperInvariant()} {certNumber} within file.");
                continue;
            }

            toAdd.Add(new SlabEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardIdentifier = cardId,
                TreatmentKey = treatment,
                GradingAgencyCode = agencyCode,
                Grade = rawGrade.Trim(),
                CertificateNumber = certNumber,
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

        var certPairs = toAdd
            .Select(e => new { e.GradingAgencyCode, e.CertificateNumber })
            .ToList();
        var existingCerts = await _db.SlabEntries
            .Where(s => s.UserId == userId)
            .Select(s => new { s.GradingAgencyCode, s.CertificateNumber })
            .ToListAsync(ct);
        var existingCertSet = existingCerts
            .Select(c => (c.GradingAgencyCode, c.CertificateNumber))
            .ToHashSet();

        int added = 0;
        foreach (var e in toAdd)
        {
            if (!existingCardSet.Contains(e.CardIdentifier))
            {
                failures.Add($"Card not found in database: {e.CardIdentifier.ToUpperInvariant()}");
                continue;
            }
            if (existingCertSet.Contains((e.GradingAgencyCode, e.CertificateNumber)))
            {
                failures.Add($"Duplicate slab: {e.GradingAgencyCode.ToUpperInvariant()} {e.CertificateNumber} already exists in your collection.");
                continue;
            }
            await _slabs.CreateAsync(e, ct);
            existingCertSet.Add((e.GradingAgencyCode, e.CertificateNumber));
            added++;
        }

        return new ImportResult(added, 0, failures.Count, failures);
    }
}
