namespace CountOrSell.Domain.Models;

// Canonical set data received via update packages.
// Stored as a flat view - no layer resolution in this project.
public class Set
{
    public string Code { get; set; } = string.Empty; // PK, ^[a-z0-9]{3,4}$, stored lowercase
    public string Name { get; set; } = string.Empty;
    public int TotalCards { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? SetType { get; set; }
    public bool Digital { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Set when a full package no longer lists this record but a user still holds it.
    // Retired records stay out of catalog surfaces (search, browse, set completion) yet
    // remain resolvable, so a user's entry keeps its name, set and treatment instead of
    // becoming an unreadable identifier. Cleared if the record reappears in a later full.
    public DateTime? RetiredAt { get; set; }
}
