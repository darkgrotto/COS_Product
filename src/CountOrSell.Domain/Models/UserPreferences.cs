namespace CountOrSell.Domain.Models;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public string? DefaultPage { get; set; }
    public bool SetCompletionRegularOnly { get; set; }
    public bool DefaultAcquisitionPriceToMarket { get; set; } = true;
    public bool DarkMode { get; set; }
    // "sidebar" or "top"
    public string NavLayout { get; set; } = "sidebar";
    // "name" or "identifier"
    public string CardSortDefault { get; set; } = "name";
    public User? User { get; set; }
}
