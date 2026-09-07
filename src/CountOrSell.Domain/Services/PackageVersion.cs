using CountOrSell.Domain.Dtos;

namespace CountOrSell.Domain.Services;

// The single version that describes a package.
//
// Manifests now carry an authoritative top-level `version`, so that is read directly.
// Already-published packages are never rewritten, so packages signed before that field
// existed still arrive without it; Resolve below reconstructs the version for those from
// the per-content versions and stays until they age out of retention.
public static class PackageVersion
{
    // Preferred entry point: the published version when the package states one, otherwise
    // the reconstruction.
    public static string? For(PackageManifest? manifest)
    {
        if (manifest == null) return null;
        return !string.IsNullOrWhiteSpace(manifest.Version)
            ? manifest.Version!.Trim()
            : Resolve(manifest.ContentVersions);
    }

    // Version of an asset bundled with the package rather than published as content - the
    // Keyrune set-symbol font today. Older packages carried these inside content_versions,
    // so that is checked second; absence from either is normal, not an error.
    public static string? BundledAssetVersion(PackageManifest? manifest, string assetKey)
    {
        if (manifest == null || string.IsNullOrWhiteSpace(assetKey)) return null;

        if (manifest.BundledAssets.TryGetValue(assetKey, out var bundled)
            && !string.IsNullOrWhiteSpace(bundled?.Version))
            return bundled!.Version.Trim();

        return manifest.ContentVersions.TryGetValue(assetKey, out var legacy)
            && !string.IsNullOrWhiteSpace(legacy?.Version)
                ? legacy!.Version.Trim()
                : null;
    }

    // Fallback for packages published before the top-level version existed.
    //
    // Those manifests carry a version per content type and no package-level one, and two
    // entries deliberately do not follow the package's value: slabs is pinned at "0.0.0"
    // (Product-managed, never published) and keyrune tracked the font's own upstream
    // version before it moved to bundled_assets. Rather than naming those exceptions this
    // takes the version the most content types agree on, which also absorbed keyrune
    // leaving content_versions without needing a change here.
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
