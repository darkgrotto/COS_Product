using CountOrSell.Domain.Models.Enums;

namespace CountOrSell.Domain.Models;

public class GradingAgency
{
    public string Code { get; set; } = string.Empty;   // stored lowercase, displayed uppercase
    public string FullName { get; set; } = string.Empty;
    public string ValidationUrlTemplate { get; set; } = string.Empty;
    public bool SupportsDirectLookup { get; set; } = true;
    public AgencySource Source { get; set; }
    public bool Active { get; set; } = true;
}
