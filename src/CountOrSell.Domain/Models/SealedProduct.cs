namespace CountOrSell.Domain.Models;

// Canonical sealed product data received via update packages.
// Stored as a flat view - no layer resolution in this project.
public class SealedProduct
{
    public string Identifier { get; set; } = string.Empty; // PK
    public string SetCode { get; set; } = string.Empty;    // FK -> sets
    public string Name { get; set; } = string.Empty;
    public string? CategorySlug { get; set; }              // FK -> sealed_product_categories (nullable, SET NULL on taxonomy removal)
    public string? SubTypeSlug { get; set; }               // FK -> sealed_product_sub_types (nullable, SET NULL on taxonomy removal)
    public decimal? CurrentMarketValue { get; set; }
    public string? ImagePath { get; set; }
    public DateTime UpdatedAt { get; set; }
}
