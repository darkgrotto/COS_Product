namespace CountOrSell.Domain.Models;

public class SetCompletionResult
{
    public string SetCode { get; set; } = string.Empty;
    public string SetName { get; set; } = string.Empty;
    public int OwnedCount { get; set; }
    public int TotalCards { get; set; }
    public decimal Percentage { get; set; }
    public decimal? TotalValue { get; set; }
    public decimal? TotalProfitLoss { get; set; }
}
