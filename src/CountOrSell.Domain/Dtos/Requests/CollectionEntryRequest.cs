namespace CountOrSell.Domain.Dtos.Requests;

public class CollectionEntryRequest
{
    public string CardIdentifier { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public bool Autographed { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public string? Notes { get; set; }
}
