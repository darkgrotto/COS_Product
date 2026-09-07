namespace CountOrSell.Domain.Services;

// Human-facing wording for a package's type. The wire value is unchanged - manifests still
// carry "delta" and "full", and all branching stays on those literals - this only controls
// what an operator reads, matching the Admin Backend's terminology.
public static class PackageTypeLabel
{
    public static string For(string? packageType) => packageType?.Trim().ToLowerInvariant() switch
    {
        "delta" => "Incremental Update",
        "full" => "Full Update",
        // An unrecognised type is shown as published rather than guessed at or hidden.
        _ => string.IsNullOrWhiteSpace(packageType) ? "Update" : packageType!,
    };
}
