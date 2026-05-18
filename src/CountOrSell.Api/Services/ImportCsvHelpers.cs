using System.Globalization;
using System.Text;
using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Services;

/// <summary>
/// Shared CSV parsing/formatting helpers for the per-entity import/export services.
/// Keeps each importer focused on its own field shape.
/// </summary>
internal static class ImportCsvHelpers
{
    public static List<List<string>> ParseCsv(TextReader reader)
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

    public static string? Col(List<string> row, List<string> header, string name)
    {
        int idx = header.IndexOf(name);
        if (idx < 0 || idx >= row.Count) return null;
        var v = row[idx].Trim();
        return v.Length == 0 ? null : v;
    }

    public static string Escape(string? v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    public static CardCondition ParseCondition(string s)
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

    public static bool TryParseCondition(string? s, out CardCondition cond)
    {
        cond = CardCondition.NM;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var n = s.Trim().ToLowerInvariant()
            .Replace(" foil", "").Replace("foil ", "").Trim();
        switch (n)
        {
            case "near mint": case "nm": case "mint": case "m":
                cond = CardCondition.NM; return true;
            case "lightly played": case "lp": case "slightly played":
            case "sp": case "light play":
                cond = CardCondition.LP; return true;
            case "moderately played": case "mp": case "moderate play":
            case "good": case "played":
                cond = CardCondition.MP; return true;
            case "heavily played": case "hp": case "heavy play":
            case "poor": case "very heavily played": case "vhp":
                cond = CardCondition.HP; return true;
            case "damaged": case "dmg":
                cond = CardCondition.DMG; return true;
            default:
                return false;
        }
    }

    public static string ConditionDisplay(CardCondition c) => c switch
    {
        CardCondition.NM  => "NM",
        CardCondition.LP  => "LP",
        CardCondition.MP  => "MP",
        CardCondition.HP  => "HP",
        CardCondition.DMG => "DMG",
        _                 => "NM",
    };

    public static bool ParseBool(string? s, bool fallback = false)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        var t = s.Trim().ToLowerInvariant();
        return t is "true" or "yes" or "y" or "1";
    }

    public static bool TryParseInt(string? s, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParsePrice(string? s, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var cleaned = s.Trim().TrimStart('$').Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy",
        "d/M/yyyy", "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy",
    ];

    public static bool TryParseDate(string? s, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        foreach (var fmt in DateFormats)
        {
            if (DateOnly.TryParseExact(s.Trim(), fmt, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
                return true;
        }
        return false;
    }
}
