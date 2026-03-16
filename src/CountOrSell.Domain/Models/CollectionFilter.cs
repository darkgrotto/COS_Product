namespace CountOrSell.Domain.Models;

public class CollectionFilter
{
    public string? SetCode { get; set; }
    public string? Color { get; set; }
    public string? Condition { get; set; }
    public string? CardType { get; set; }
    public string? Treatment { get; set; }
    public bool? Autographed { get; set; }
    public bool? Serialized { get; set; }
    public bool? Slabbed { get; set; }
    public bool? SealedProduct { get; set; }
    public string? GradingAgency { get; set; }
}
