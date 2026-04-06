using System.Text.Json.Serialization;

namespace CountOrSell.Domain.Dtos;

// Manifest fetched from www.countorsell.com/updates/manifest.json
// Update source is always www.countorsell.com - not configurable.
public class UpdateManifest
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("minimum_product_version")]
    public string MinimumProductVersion { get; set; } = string.Empty;

    [JsonPropertyName("content_versions")]
    public Dictionary<string, ContentVersionEntry> ContentVersions { get; set; } = new();

    [JsonPropertyName("packages")]
    public List<UpdatePackage> Packages { get; set; } = new();
}

public class ContentVersionEntry
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("record_count")]
    public int? RecordCount { get; set; }
}

// A downloadable update package. Type is "content" or "schema".
public class UpdatePackage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("zip_sha256")]
    public string ZipSha256 { get; set; } = string.Empty;

    [JsonPropertyName("minimum_schema_version")]
    public int MinimumSchemaVersion { get; set; }
}
