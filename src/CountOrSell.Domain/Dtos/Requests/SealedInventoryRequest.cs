namespace CountOrSell.Domain.Dtos.Requests;

public class SealedInventoryRequest
{
    public string ProductIdentifier { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public string? Notes { get; set; }
    public string? CategorySlug { get; set; }
    public string? SubTypeSlug { get; set; }
}
