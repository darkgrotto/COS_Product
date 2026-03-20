using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class DemoModeService : IDemoModeService
{
    private static readonly IReadOnlyList<string> _allDemoSets =
        new[]
        {
            "lea", "2ed", "vis", "eoe", "fdn", "ecl",
            "tla", "fin", "dsk", "usg", "ulg", "uns", "p23", "tdm",
        };

    public bool IsDemo { get; }
    public IReadOnlyList<string> DemoSets => IsDemo ? _allDemoSets : Array.Empty<string>();
    public DateTimeOffset? ExpiresAt { get; }

    public int SecondsRemaining
    {
        get
        {
            if (!IsDemo || !ExpiresAt.HasValue) return 0;
            var remaining = (int)(ExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds;
            return remaining < 0 ? 0 : remaining;
        }
    }

    public DemoModeService(IConfiguration config)
    {
        var demoMode = config["DEMO_MODE"];
        IsDemo = string.Equals(demoMode, "true", StringComparison.OrdinalIgnoreCase);

        if (IsDemo)
        {
            var expiresAtStr = config["DEMO_EXPIRES_AT"];
            if (!string.IsNullOrWhiteSpace(expiresAtStr) &&
                DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
            {
                ExpiresAt = expiresAt;
            }
        }
    }
}
