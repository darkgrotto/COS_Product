using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos;

// Website manifest fetched from www.countorsell.com/updates/manifest.json
// Update source is always www.countorsell.com - not configurable.
public class UpdateManifest
{
    // Overall package version. Added alongside the per-content versions; absent on
    // manifests generated before that change.
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("minimum_product_version")]
    public string MinimumProductVersion { get; set; } = string.Empty;

    [JsonPropertyName("content_versions")]
    public Dictionary<string, ContentVersionEntry> ContentVersions { get; set; } = new();

    [JsonPropertyName("packages")]
    public List<UpdatePackageRef> Packages { get; set; } = new();
}

public class ContentVersionEntry
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("record_count")]
    public int? RecordCount { get; set; }
}

// A reference to a downloadable update package listed in the website manifest.
// Type is "full" or "delta". Delta packages are cumulative from base_full_version.
public class UpdatePackageRef
{
    [JsonPropertyName("package_id")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("package_type")]
    public string PackageType { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("manifest_url")]
    public string ManifestUrl { get; set; } = string.Empty;

    [JsonPropertyName("base_full_version")]
    public string? BaseFullVersion { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTimeOffset GeneratedAt { get; set; }

    // The package's own version, so a package can be selected straight from the index
    // without fetching its manifest. Absent on entries generated before that change.
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

// Per-package manifest fetched from manifest_url or extracted from the ZIP as manifest.json.
// Contains per-file checksums (format: "sha256:<hex_lowercase>") and content version info.
public class PackageManifest
{
    // The authoritative overall package version, tracking the cards lineage: every publish
    // advances the published content types in lockstep with it. Absent on packages signed
    // before the field was introduced - those are not rewritten, so the fallback in
    // PackageVersion stays until they age out of retention.
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("package_type")]
    public string PackageType { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("base_full_version")]
    public string? BaseFullVersion { get; set; }

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("content_versions")]
    public Dictionary<string, ContentVersionEntry> ContentVersions { get; set; } = new();

    // Versions of assets bundled with the package rather than published as content -
    // currently the Keyrune set-symbol font. Deliberately outside content_versions: these
    // track their own upstream and never participate in the package version. Absent on
    // older packages, where keyrune still sits inside content_versions.
    [JsonPropertyName("bundled_assets")]
    public Dictionary<string, ContentVersionEntry> BundledAssets { get; set; } = new();

    [JsonPropertyName("retained_full_versions")]
    public List<string> RetainedFullVersions { get; set; } = new();

    // Key: file path within ZIP (e.g. "metadata/treatments.json", "metadata/sets/eoe/cards.json")
    // Value: checksum in format "sha256:<hex_lowercase>"
    [JsonPropertyName("checksums")]
    public Dictionary<string, string> Checksums { get; set; } = new();
}
