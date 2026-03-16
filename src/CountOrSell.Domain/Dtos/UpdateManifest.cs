namespace CountOrSell.Domain.Dtos;

// Manifest fetched from countorsell.com/updates/manifest.json
// Update source is always countorsell.com - not configurable.
public class UpdateManifest
{
    public ContentManifestEntry Content { get; set; } = new();
    public SchemaManifestEntry? Schema { get; set; }
    public ApplicationManifestEntry? Application { get; set; }
}

public class ContentManifestEntry
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ZipSha256 { get; set; } = string.Empty;
    public int MinimumProductSchemaVersion { get; set; }
}

public class SchemaManifestEntry
{
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ZipSha256 { get; set; } = string.Empty;
}

public class ApplicationManifestEntry
{
    public string Version { get; set; } = string.Empty;
}
