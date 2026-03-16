namespace CountOrSell.Domain.Models;

// Canonical set data received via update packages.
// Stored as a flat view - no layer resolution in this project.
public class Set
{
    public string Code { get; set; } = string.Empty; // PK, ^[a-z0-9]{3,4}$, stored lowercase
    public string Name { get; set; } = string.Empty;
    public int TotalCards { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}
