namespace CountOrSell.Domain;

/// <summary>
/// Extracts the comma-joined subtype list from a Scryfall-style type_line.
/// Handles double-faced cards (faces separated by " // ") and returns null
/// when no subtypes are present.
/// </summary>
public static class CardTypeLineParser
{
    // U+2014 EM DASH - the literal separator Scryfall uses between
    // supertypes/types and subtypes (e.g. "Creature — Human Wizard").
    private const char TypeDash = '—';
    private static readonly string FaceSeparator = " // ";

    /// <summary>
    /// Returns subtypes from <paramref name="typeLine"/> as a comma-joined string in
    /// first-occurrence order, deduplicated across faces. Returns null if there are
    /// no subtypes (no dash, or the input is null/empty).
    /// </summary>
    public static string? ExtractSubtypes(string? typeLine)
    {
        if (string.IsNullOrWhiteSpace(typeLine)) return null;

        var subtypes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var face in typeLine.Split(FaceSeparator, StringSplitOptions.None))
        {
            var dashIdx = face.IndexOf(TypeDash);
            if (dashIdx < 0) continue;

            var after = face[(dashIdx + 1)..].Trim();
            if (after.Length == 0) continue;

            foreach (var token in after.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (seen.Add(token)) subtypes.Add(token);
            }
        }

        return subtypes.Count == 0 ? null : string.Join(",", subtypes);
    }
}
