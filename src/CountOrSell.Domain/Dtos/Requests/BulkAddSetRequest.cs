namespace CountOrSell.Domain.Dtos.Requests;

public class BulkAddSetRequest
{
    public string SetCode { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
}
