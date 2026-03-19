namespace CountOrSell.Domain.Models;

// Received via update packages, versioned independently.
// Slugs are primary keys (no integer IDs).
public class SealedProductCategory
{
    public string Slug { get; set; } = string.Empty; // PK
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
