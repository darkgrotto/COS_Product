namespace CountOrSell.Domain.Dtos.Requests;

public class BulkAddSetsRequest
{
    public List<string> SetCodes { get; set; } = new();
    public string Treatment { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
}
