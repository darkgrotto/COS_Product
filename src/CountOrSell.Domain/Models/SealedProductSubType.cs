namespace CountOrSell.Domain.Models;

// Received via update packages, versioned independently.
// Slugs are primary keys (no integer IDs).
public class SealedProductSubType
{
    public string Slug { get; set; } = string.Empty;          // PK
    public string CategorySlug { get; set; } = string.Empty;  // FK -> sealed_product_categories
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
