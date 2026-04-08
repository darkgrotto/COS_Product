namespace CountOrSell.Domain.Dtos.Requests;

public class WishlistRequest
{
    public string CardIdentifier { get; set; } = string.Empty;
    public string TreatmentKey { get; set; } = "regular";
}
