namespace CountOrSell.Domain.Models;

public class MetricsResult
{
    public decimal TotalValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public int TotalCardCount { get; set; }
    public int SerializedCount { get; set; }
    public int SlabCount { get; set; }
    public int SealedProductCount { get; set; }
    public decimal SealedProductValue { get; set; }
    public List<ContentTypeBreakdown> ByContentType { get; set; } = new();
}

public class ContentTypeBreakdown
{
    public string ContentType { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public int Count { get; set; }
}
