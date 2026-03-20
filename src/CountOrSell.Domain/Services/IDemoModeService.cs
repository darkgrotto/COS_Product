namespace CountOrSell.Domain.Services;

public interface IDemoModeService
{
    bool IsDemo { get; }
    IReadOnlyList<string> DemoSets { get; }
    DateTimeOffset? ExpiresAt { get; }
    int SecondsRemaining { get; }
}
