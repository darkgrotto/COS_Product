using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public sealed class CollectionImportExportService : ICollectionImportExportService
{
    private readonly AppDbContext _db;
    private readonly ICollectionRepository _collection;

    public CollectionImportExportService(AppDbContext db, ICollectionRepository collection)
    {
        _db = db;
        _collection = collection;
    }

    // ---- Export -----------------------------------------------------------------

    public async Task<(byte[] Data, string FileName)> ExportAsync(
        Guid userId,
        CollectionExportFormat format,
        CancellationToken ct = default)
    {
        var entries = await _collection.GetByUserAsync(userId, ct);
        return await BuildExportAsync(entries, format, ct);
    }

    public async Task<(byte[] Data, string FileName)> ExportFilteredAsync(
        Guid userId,
        CollectionExportFormat format,
        CollectionFilter filter,
        CancellationToken ct = default)
    {
        var entries = await _collection.GetByUserFilteredAsync(userId, filter, ct);
        return await BuildExportAsync(entries, format, ct);
    }

    private async Task<(byte[] Data, string FileName)> BuildExportAsync(
        List<CollectionEntry> entries,
        CollectionExportFormat format,
        CancellationToken ct)
    {
        var identifiers = entries.Select(e => e.CardIdentifier).Distinct().ToList();

        var cardMap = await _db.Cards
            .Where(c => identifiers.Contains(c.Identifier))
            .ToDictionaryAsync(c => c.Identifier, ct);

        var setCodeMap = await _db.Sets
            .ToDictionaryAsync(s => s.Code, s => s.Name, ct);

        var treatmentMap = await _db.Treatments
            .ToDictionaryAsync(t => t.Key, t => t.DisplayName, ct);

        var sb = new StringBuilder();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        string fileName = format switch
        {
            CollectionExportFormat.Moxfield    => $"moxfield-collection-{date}.csv",
            CollectionExportFormat.Deckbox     => $"deckbox-collection-{date}.csv",
            CollectionExportFormat.TcgPlayer   => $"tcgplayer-collection-{date}.csv",
            CollectionExportFormat.DragonShield => $"dragonshield-collection-{date}.csv",
            CollectionExportFormat.Manabox     => $"manabox-collection-{date}.csv",
            _                                  => $"cos-collection-{date}.csv",
        };

        switch (format)
        {
            case CollectionExportFormat.Cos:
                WriteCos(sb, entries, cardMap, treatmentMap);
                break;
            case CollectionExportFormat.Moxfield:
                WriteMoxfield(sb, entries, cardMap, treatmentMap);
                break;
            case CollectionExportFormat.Deckbox:
                WriteDeckbox(sb, entries, cardMap, setCodeMap, treatmentMap);
                break;
            case CollectionExportFormat.TcgPlayer:
                WriteTcgPlayer(sb, entries, cardMap, setCodeMap);
                break;
            case CollectionExportFormat.DragonShield:
                WriteDragonShield(sb, entries, cardMap, treatmentMap);
                break;
            case CollectionExportFormat.Manabox:
                WriteManabox(sb, entries, cardMap);
                break;
        }

        return (Encoding.UTF8.GetBytes(sb.ToString()), fileName);
    }

    // ---- COS export -------------------------------------------------------------

    static void WriteCos(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards,
        Dictionary<string, string> treatments)
    {
        sb.AppendLine("Identifier,Name,SetCode,Treatment,Quantity,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            sb.Append(E(e.CardIdentifier)); sb.Append(',');
            sb.Append(E(card?.Name));        sb.Append(',');
            sb.Append(E(card?.SetCode));     sb.Append(',');
            sb.Append(E(e.TreatmentKey));    sb.Append(',');
            sb.Append(e.Quantity);           sb.Append(',');
            sb.Append(e.Condition);          sb.Append(',');
            sb.Append(e.Autographed ? "true" : "false"); sb.Append(',');
            sb.Append(e.AcquisitionDate.ToString("yyyy-MM-dd")); sb.Append(',');
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.AppendLine(E(e.Notes));
        }
    }

    // ---- Moxfield export --------------------------------------------------------

    static void WriteMoxfield(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards,
        Dictionary<string, string> treatments)
    {
        sb.AppendLine("Count,Tradelist Count,Name,Edition,Condition,Language,Foil,Tags,Last Modified,Collector Number,Alter,Proxy,Purchase Price");
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            var setCode = card?.SetCode?.ToUpperInvariant() ?? string.Empty;
            var collNum = CollectorNumber(e.CardIdentifier, card?.SetCode ?? string.Empty);
            var condition = ConditionDisplay(e.Condition);
            var isFoil = e.TreatmentKey == "foil";
            var otherTreatment = (!isFoil && e.TreatmentKey != "regular")
                ? e.TreatmentKey : string.Empty;

            sb.Append(e.Quantity);    sb.Append(",,");
            sb.Append(E(card?.Name)); sb.Append(',');
            sb.Append(E(setCode));    sb.Append(',');
            sb.Append(E(condition));  sb.Append(',');
            sb.Append("English,");
            sb.Append(isFoil ? "foil" : string.Empty); sb.Append(',');
            sb.Append(E(otherTreatment)); sb.Append(',');
            sb.Append(today);         sb.Append(',');
            sb.Append(E(collNum));    sb.Append(",,,$");
            sb.AppendLine(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture));
        }
    }

    // ---- Deckbox export ---------------------------------------------------------

    static void WriteDeckbox(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards,
        Dictionary<string, string> setCodeToName,
        Dictionary<string, string> treatments)
    {
        sb.AppendLine("Count,Tradelist Count,Name,Edition,Card Number,Condition,Language,Foil,Signed,Artist Proof,Altered Art,Misprint,Promo,Textless,My Price");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            setCodeToName.TryGetValue(card?.SetCode ?? string.Empty, out var setName);
            var collNum = CollectorNumber(e.CardIdentifier, card?.SetCode ?? string.Empty);
            var condition = ConditionDisplay(e.Condition);
            var isFoil = e.TreatmentKey != "regular";
            var isSigned = e.Autographed;

            sb.Append(e.Quantity);          sb.Append(",,");
            sb.Append(E(card?.Name));        sb.Append(',');
            sb.Append(E(setName ?? card?.SetCode?.ToUpperInvariant())); sb.Append(',');
            sb.Append(E(collNum));           sb.Append(',');
            sb.Append(E(condition));         sb.Append(',');
            sb.Append("English,");
            sb.Append(isFoil   ? "foil"   : string.Empty); sb.Append(',');
            sb.Append(isSigned ? "signed" : string.Empty); sb.Append(",,,,,,,$");
            sb.AppendLine(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture));
        }
    }

    // ---- TCGPlayer export -------------------------------------------------------

    static void WriteTcgPlayer(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards,
        Dictionary<string, string> setCodeToName)
    {
        sb.AppendLine("Quantity,Product Line,Set Name,Product Name,Title,Number,Rarity,Condition,TCG Market Price,TCG Direct Low,TCG Low Price With Shipping,TCG Low Price,Total Quantity,Add to Quantity,TCG Marketplace Price,Photo URL");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            setCodeToName.TryGetValue(card?.SetCode ?? string.Empty, out var setName);
            var collNum = CollectorNumber(e.CardIdentifier, card?.SetCode ?? string.Empty);
            var condition = ConditionDisplay(e.Condition);
            var isFoil = e.TreatmentKey != "regular";
            var fullCondition = isFoil ? $"{condition} Foil" : condition;

            sb.Append(e.Quantity);    sb.Append(',');
            sb.Append("Magic,");
            sb.Append(E(setName ?? card?.SetCode?.ToUpperInvariant())); sb.Append(',');
            sb.Append(E(card?.Name)); sb.Append(",,");
            sb.Append(E(collNum));    sb.Append(",,");
            sb.Append(E(fullCondition)); sb.Append(',');
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture));
            sb.AppendLine(",,,,,,,");
        }
    }

    // ---- Dragon Shield export ---------------------------------------------------

    static void WriteDragonShield(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards,
        Dictionary<string, string> treatments)
    {
        sb.AppendLine("Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Card Number,Condition,Printing,Language,Price Bought,Date Bought");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            var setCode = card?.SetCode?.ToUpperInvariant() ?? string.Empty;
            var collNum = CollectorNumber(e.CardIdentifier, card?.SetCode ?? string.Empty);
            var condition = ConditionDisplay(e.Condition);
            treatments.TryGetValue(e.TreatmentKey, out var treatDisplay);
            var printing = e.TreatmentKey == "foil" ? "Foil"
                         : e.TreatmentKey == "regular" ? "Normal"
                         : treatDisplay ?? "Normal";

            sb.Append(',');
            sb.Append(e.Quantity);           sb.Append(",,");
            sb.Append(E(card?.Name));        sb.Append(',');
            sb.Append(E(setCode));           sb.Append(',');
            sb.Append(E(collNum));           sb.Append(',');
            sb.Append(E(condition));         sb.Append(',');
            sb.Append(E(printing));          sb.Append(',');
            sb.Append("English,");
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.AppendLine(e.AcquisitionDate.ToString("yyyy-MM-dd"));
        }
    }

    // ---- Manabox export ---------------------------------------------------------

    static void WriteManabox(
        StringBuilder sb,
        List<CollectionEntry> entries,
        Dictionary<string, Card> cards)
    {
        sb.AppendLine("Name,Set code,Collector number,Foil,Condition,Language,Purchase price,Current Value,Alter,Misprint,Signed,Altered,Notes");
        foreach (var e in entries)
        {
            cards.TryGetValue(e.CardIdentifier, out var card);
            var setCode  = card?.SetCode?.ToUpperInvariant() ?? string.Empty;
            var collNum  = CollectorNumber(e.CardIdentifier, card?.SetCode ?? string.Empty);
            var foilVal  = e.TreatmentKey switch
            {
                "foil"         => "foil",
                "etched-foil"  => "etched",
                _              => "normal",
            };
            var condition = ConditionDisplay(e.Condition);
            var signed    = e.Autographed ? "true" : "false";

            sb.Append(E(card?.Name));    sb.Append(',');
            sb.Append(E(setCode));       sb.Append(',');
            sb.Append(E(collNum));       sb.Append(',');
            sb.Append(foilVal);          sb.Append(',');
            sb.Append(E(condition));     sb.Append(',');
            sb.Append("English,");
            sb.Append(e.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture)); sb.Append(',');
            sb.Append(',');              // Current Value (blank on export)
            sb.Append(",,");             // Alter, Misprint
            sb.Append(signed);           sb.Append(',');
            sb.Append(',');              // Altered
            sb.AppendLine(E(e.Notes));
        }
    }

    // ---- Import -----------------------------------------------------------------

    public async Task<ImportResult> ImportAsync(
        Guid userId,
        CollectionExportFormat format,
        Stream stream,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var allRows = ParseCsv(reader);
        if (allRows.Count < 2)
            return new ImportResult(0, 0, 0, []);

        var header = allRows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        var dataRows = allRows.Skip(1).ToList();

        // For set-name-based formats, build name -> code lookup
        Dictionary<string, string>? setNameToCode = null;
        if (format is CollectionExportFormat.Deckbox or CollectionExportFormat.TcgPlayer)
        {
            setNameToCode = await _db.Sets
                .ToDictionaryAsync(s => s.Name.ToLowerInvariant(), s => s.Code, ct);
        }

        var toAdd = new List<CollectionEntry>();
        var failures = new List<string>();

        foreach (var (row, rowIdx) in dataRows.Select((r, i) => (r, i + 2)))
        {
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            string? cardIdentifier = null;
            int quantity = 1;
            string conditionStr = "NM";
            string treatmentKey = "regular";
            bool autographed = false;
            DateOnly acquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow);
            decimal acquisitionPrice = 0m;
            string? notes = null;
            string? rowLabel = null;

            try
            {
                switch (format)
                {
                    case CollectionExportFormat.Cos:
                        cardIdentifier = Col(row, header, "identifier")?.Trim().ToLowerInvariant();
                        quantity       = ParseInt(Col(row, header, "quantity"), 1);
                        conditionStr   = Col(row, header, "condition") ?? "NM";
                        treatmentKey   = Col(row, header, "treatment") ?? "regular";
                        autographed    = Col(row, header, "autographed")?.Trim().ToLowerInvariant() == "true";
                        acquisitionDate = ParseDate(Col(row, header, "acquisitiondate"));
                        acquisitionPrice = ParsePrice(Col(row, header, "acquisitionprice"));
                        notes          = Col(row, header, "notes");
                        rowLabel       = cardIdentifier?.ToUpperInvariant();
                        break;

                    case CollectionExportFormat.Moxfield:
                    {
                        var setCode  = Col(row, header, "edition")?.Trim().ToLowerInvariant() ?? string.Empty;
                        var collNum  = Col(row, header, "collector number")?.Trim() ?? string.Empty;
                        var name     = Col(row, header, "name")?.Trim();
                        quantity     = ParseInt(Col(row, header, "count"), 1);
                        conditionStr = Col(row, header, "condition") ?? "NM";
                        var foilStr  = Col(row, header, "foil")?.Trim().ToLowerInvariant();
                        treatmentKey = (foilStr == "foil") ? "foil" : "regular";
                        acquisitionPrice = ParsePrice(Col(row, header, "purchase price"));
                        rowLabel     = $"{name} ({setCode.ToUpperInvariant()} {collNum})";

                        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(collNum))
                            cardIdentifier = setCode + NormalizeCollNum(collNum);
                        break;
                    }

                    case CollectionExportFormat.Deckbox:
                    {
                        var setName  = Col(row, header, "edition")?.Trim() ?? string.Empty;
                        var collNum  = Col(row, header, "card number")?.Trim() ?? string.Empty;
                        var name     = Col(row, header, "name")?.Trim();
                        quantity     = ParseInt(Col(row, header, "count"), 1);
                        conditionStr = Col(row, header, "condition") ?? "NM";
                        var foilStr  = Col(row, header, "foil")?.Trim().ToLowerInvariant();
                        treatmentKey = (foilStr == "foil") ? "foil" : "regular";
                        var signedStr = Col(row, header, "signed")?.Trim().ToLowerInvariant();
                        autographed  = signedStr == "signed" || signedStr == "true";
                        acquisitionPrice = ParsePrice(Col(row, header, "my price"));
                        rowLabel     = $"{name} ({setName} {collNum})";

                        if (setNameToCode != null && !string.IsNullOrEmpty(setName) && !string.IsNullOrEmpty(collNum))
                        {
                            var setCode = ResolveSetName(setName, setNameToCode);
                            if (setCode != null)
                                cardIdentifier = setCode + NormalizeCollNum(collNum);
                        }
                        break;
                    }

                    case CollectionExportFormat.TcgPlayer:
                    {
                        var setName  = Col(row, header, "set name")?.Trim() ?? string.Empty;
                        var collNum  = (Col(row, header, "number") ?? Col(row, header, "title") ?? string.Empty).Trim();
                        var name     = Col(row, header, "product name")?.Trim();
                        quantity     = ParseInt(Col(row, header, "quantity"), 1);
                        var rawCond  = Col(row, header, "condition") ?? "Near Mint";
                        var foilInCond = rawCond.ToLowerInvariant().Contains("foil");
                        conditionStr = rawCond.ToLowerInvariant().Replace(" foil", "").Replace("foil ", "").Trim();
                        treatmentKey = foilInCond ? "foil" : "regular";
                        acquisitionPrice = ParsePrice(Col(row, header, "tcg market price")
                                         ?? Col(row, header, "tcg marketplace price"));
                        rowLabel = $"{name} ({setName} {collNum})";

                        if (setNameToCode != null && !string.IsNullOrEmpty(setName) && !string.IsNullOrEmpty(collNum))
                        {
                            var setCode = ResolveSetName(setName, setNameToCode);
                            if (setCode != null)
                                cardIdentifier = setCode + NormalizeCollNum(collNum);
                        }
                        break;
                    }

                    case CollectionExportFormat.DragonShield:
                    {
                        var setCode  = Col(row, header, "set code")?.Trim().ToLowerInvariant() ?? string.Empty;
                        var collNum  = Col(row, header, "card number")?.Trim() ?? string.Empty;
                        var name     = Col(row, header, "card name")?.Trim();
                        quantity     = ParseInt(Col(row, header, "quantity"), 1);
                        conditionStr = Col(row, header, "condition") ?? "Near Mint";
                        var printing = Col(row, header, "printing")?.Trim().ToLowerInvariant() ?? "normal";
                        treatmentKey = printing == "foil" ? "foil" : "regular";
                        acquisitionPrice = ParsePrice(Col(row, header, "price bought"));
                        acquisitionDate  = ParseDate(Col(row, header, "date bought"));
                        rowLabel         = $"{name} ({setCode.ToUpperInvariant()} {collNum})";

                        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(collNum))
                            cardIdentifier = setCode + NormalizeCollNum(collNum);
                        break;
                    }

                    case CollectionExportFormat.Manabox:
                    {
                        var setCode  = Col(row, header, "set code")?.Trim().ToLowerInvariant() ?? string.Empty;
                        var collNum  = Col(row, header, "collector number")?.Trim() ?? string.Empty;
                        var name     = Col(row, header, "name")?.Trim();
                        quantity     = 1; // Manabox exports one row per card
                        conditionStr = Col(row, header, "condition") ?? "Near Mint";
                        var foilStr  = Col(row, header, "foil")?.Trim().ToLowerInvariant() ?? "normal";
                        treatmentKey = foilStr switch
                        {
                            "foil"   => "foil",
                            "etched" => "etched-foil",
                            _        => "regular",
                        };
                        var signedStr = Col(row, header, "signed")?.Trim().ToLowerInvariant();
                        autographed  = signedStr == "true";
                        acquisitionPrice = ParsePrice(Col(row, header, "purchase price"));
                        rowLabel         = $"{name} ({setCode.ToUpperInvariant()} {collNum})";

                        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(collNum))
                            cardIdentifier = setCode + NormalizeCollNum(collNum);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(cardIdentifier))
                {
                    failures.Add($"Row {rowIdx}: could not resolve card identifier for {rowLabel ?? "(unknown)"}");
                    continue;
                }

                // Validate identifier format loosely before DB lookup
                if (!Regex.IsMatch(cardIdentifier, @"^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$"))
                {
                    failures.Add($"Row {rowIdx}: invalid identifier '{cardIdentifier.ToUpperInvariant()}'");
                    continue;
                }

                toAdd.Add(new CollectionEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CardIdentifier = cardIdentifier,
                    TreatmentKey = string.IsNullOrEmpty(treatmentKey) ? "regular" : treatmentKey,
                    Quantity = Math.Max(1, quantity),
                    Condition = ParseCondition(conditionStr),
                    Autographed = autographed,
                    AcquisitionDate = acquisitionDate,
                    AcquisitionPrice = acquisitionPrice,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                failures.Add($"Row {rowIdx}: {ex.Message}");
            }
        }

        if (toAdd.Count == 0)
            return new ImportResult(0, 0, failures.Count, failures);

        // Verify card identifiers exist in DB - batch check
        var requestedIds = toAdd.Select(e => e.CardIdentifier).ToHashSet();
        var existingIdsList = await _db.Cards
            .Where(c => requestedIds.Contains(c.Identifier))
            .Select(c => c.Identifier)
            .ToListAsync(ct);
        var existingIds = existingIdsList.ToHashSet();

        var valid = toAdd.Where(e => existingIds.Contains(e.CardIdentifier)).ToList();
        var notFound = toAdd.Where(e => !existingIds.Contains(e.CardIdentifier)).ToList();

        foreach (var e in notFound)
            failures.Add($"Card not found in database: {e.CardIdentifier.ToUpperInvariant()}");

        if (valid.Count > 0)
            await _collection.BulkCreateAsync(valid, ct);

        return new ImportResult(valid.Count, 0, failures.Count, failures);
    }

    // ---- CSV helpers ------------------------------------------------------------

    static List<List<string>> ParseCsv(TextReader reader)
    {
        var rows = new List<List<string>>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQ)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    { cur.Append('"'); i++; }
                    else if (c == '"')
                    { inQ = false; }
                    else
                    { cur.Append(c); }
                }
                else
                {
                    if (c == '"') { inQ = true; }
                    else if (c == ',') { fields.Add(cur.ToString()); cur.Clear(); }
                    else { cur.Append(c); }
                }
            }
            fields.Add(cur.ToString());
            rows.Add(fields);
        }
        return rows;
    }

    static string? Col(List<string> row, List<string> header, string name)
    {
        int idx = header.IndexOf(name);
        if (idx < 0) return null;
        if (idx >= row.Count) return null;
        var v = row[idx].Trim();
        return v.Length == 0 ? null : v;
    }

    static string E(string? v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    // ---- Data helpers -----------------------------------------------------------

    static string CollectorNumber(string identifier, string setCode)
    {
        if (string.IsNullOrEmpty(setCode) || identifier.Length <= setCode.Length)
            return identifier;
        return identifier.Substring(setCode.Length);
    }

    static string NormalizeCollNum(string cn)
    {
        cn = cn.Trim().ToLowerInvariant();
        // Strip leading symbols like "#"
        cn = cn.TrimStart('#');
        var m = Regex.Match(cn, @"^(\d+)([a-z]?)$");
        if (!m.Success) return cn;
        if (!int.TryParse(m.Groups[1].Value, out int n)) return cn;
        var letter = m.Groups[2].Value;
        return (n >= 1000 ? n.ToString() : n.ToString("D3")) + letter;
    }

    static string ConditionDisplay(CardCondition c) => c switch
    {
        CardCondition.NM  => "Near Mint",
        CardCondition.LP  => "Lightly Played",
        CardCondition.MP  => "Moderately Played",
        CardCondition.HP  => "Heavily Played",
        CardCondition.DMG => "Damaged",
        _                 => "Near Mint",
    };

    static CardCondition ParseCondition(string s)
    {
        var n = s.Trim().ToLowerInvariant()
            .Replace(" foil", "").Replace("foil ", "").Trim();
        return n switch
        {
            "near mint" or "nm" or "mint" or "m"         => CardCondition.NM,
            "lightly played" or "lp" or "slightly played"
                or "sp" or "light play"                   => CardCondition.LP,
            "moderately played" or "mp" or "moderate play"
                or "good" or "played"                     => CardCondition.MP,
            "heavily played" or "hp" or "heavy play"
                or "poor" or "very heavily played" or "vhp" => CardCondition.HP,
            "damaged" or "dmg"                            => CardCondition.DMG,
            _ => CardCondition.NM,
        };
    }

    static int ParseInt(string? s, int fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        return int.TryParse(s.Trim(), out int v) ? Math.Max(1, v) : fallback;
    }

    static decimal ParsePrice(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        var cleaned = s.Trim().TrimStart('$').Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            ? Math.Max(0m, v) : 0m;
    }

    static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy",
        "d/M/yyyy", "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy",
    ];

    static DateOnly ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var fmt in DateFormats)
        {
            if (DateOnly.TryParseExact(s.Trim(), fmt, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
                return d;
        }
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>Case-insensitive set name resolution with fuzzy substring fallback.</summary>
    static string? ResolveSetName(string setName, Dictionary<string, string> nameToCode)
    {
        var lower = setName.ToLowerInvariant();
        if (nameToCode.TryGetValue(lower, out var exact)) return exact;

        // Try partial match (longer set name containing the query or vice versa)
        foreach (var (name, code) in nameToCode)
        {
            if (name.Contains(lower) || lower.Contains(name))
                return code;
        }
        return null;
    }
}
