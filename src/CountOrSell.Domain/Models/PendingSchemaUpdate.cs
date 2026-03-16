namespace CountOrSell.Domain.Models;

// A schema update detected from the manifest that requires admin approval
// before it can be applied.
public class PendingSchemaUpdate
{
    public int Id { get; set; }
    public string SchemaVersion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ZipSha256 { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
}
