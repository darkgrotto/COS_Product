namespace CountOrSell.Domain.Dtos.Requests;

public class GradingAgencyRequest
{
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ValidationUrlTemplate { get; set; } = string.Empty;
    public bool SupportsDirectLookup { get; set; } = true;
}
