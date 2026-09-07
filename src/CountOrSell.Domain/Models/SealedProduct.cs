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
    public string? Upc { get; set; }                       // UPC-A (12 digits) or EAN-13 (13 digits), optional
    public decimal? CurrentMarketValue { get; set; }
    public string? ImagePath { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Set when a full package no longer lists this record but a user still holds it.
    // Retired records stay out of catalog surfaces (search, browse, set completion) yet
    // remain resolvable, so a user's entry keeps its name, set and treatment instead of
    // becoming an unreadable identifier. Cleared if the record reappears in a later full.
    public DateTime? RetiredAt { get; set; }
}
