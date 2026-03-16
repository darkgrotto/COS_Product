namespace CountOrSell.Domain.Models;

// Records each successfully applied content update package.
public class UpdateVersion
{
    public int Id { get; set; }
    public string ContentVersion { get; set; } = string.Empty;
    public string? SchemaVersion { get; set; }
    public string? ApplicationVersion { get; set; }
    public DateTime AppliedAt { get; set; }
}
