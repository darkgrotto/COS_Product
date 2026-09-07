using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

// The single version that describes a package.
//
// A manifest carries a version per content type rather than one for the package, and in
// practice the Backend stamps the same value across all of them. Two entries deliberately
// do not follow that value and must not be mistaken for it:
//   - slabs is always "0.0.0" - Product-managed content that is never published
//   - keyrune tracks the font's own upstream version (e.g. "3.19.0")
// Rather than naming those exceptions, this takes the version the most content types agree
// on, which stays correct if the Backend adds another independently-versioned entry.
public static class PackageVersion
{
    public static string? Resolve(IReadOnlyDictionary<string, ContentVersionEntry>? contentVersions)
    {
        if (contentVersions == null || contentVersions.Count == 0) return null;

        var candidates = contentVersions
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value?.Version))
            .Select(kvp => new { Key = kvp.Key, Version = kvp.Value!.Version.Trim() })
            .ToList();

        if (candidates.Count == 0) return null;

        var byFrequency = candidates
            .GroupBy(c => c.Version, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Version = g.Key,
                Count = g.Count(),
                // Cards is the headline content type, so it breaks a tie between two
                // versions that appear equally often.
                HasCards = g.Any(c => string.Equals(c.Key, "cards", StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.HasCards)
            .ThenByDescending(g => g.Version, StringComparer.Ordinal)
            .ToList();

        return byFrequency[0].Version;
    }
}
