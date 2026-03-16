namespace CountOrSell.Domain.Dtos.Requests;

public class GradingAgencyPatchRequest
{
    public string? FullName { get; set; }
    public string? ValidationUrlTemplate { get; set; }
    public bool? SupportsDirectLookup { get; set; }
}
