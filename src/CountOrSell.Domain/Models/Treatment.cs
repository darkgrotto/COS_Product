namespace CountOrSell.Domain.Models;

public class Treatment
{
    public string Key { get; set; } = string.Empty;   // e.g. "foil", "surge-foil"
    public string DisplayName { get; set; } = string.Empty; // e.g. "Foil", "Surge Foil"
    public int SortOrder { get; set; }
}
