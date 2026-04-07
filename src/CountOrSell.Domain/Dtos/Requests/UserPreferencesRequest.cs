namespace CountOrSell.Domain.Dtos.Requests;

public class UserPreferencesRequest
{
    public bool? SetCompletionRegularOnly { get; set; }
    public string? DefaultPage { get; set; }
    public bool? DefaultAcquisitionPriceToMarket { get; set; }
}
