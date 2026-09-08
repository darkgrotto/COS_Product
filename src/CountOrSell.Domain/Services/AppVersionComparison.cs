namespace CountOrSell.Domain.Services;

// Whether a released version is actually newer than the running one.
//
// This was string inequality, which reports an update whenever the two differ - including
// when the instance is ahead. An instance on 1.2.1 whose cached "latest" is still 1.2.0
// (the release check failed, or has not run since the release) advertised an update to an
// older version. String comparison is also wrong across digit widths: "1.10.0" sorts below
// "1.9.0" as text.
public static class AppVersionComparison
{
    public static bool IsUpdateAvailable(string? current, string? latest)
    {
        if (string.IsNullOrWhiteSpace(latest) || string.IsNullOrWhiteSpace(current)) return false;

        // An unparseable version means we cannot tell. Staying quiet is better than
        // claiming an update that may not exist - the About view still shows both
        // strings, so a person can see the discrepancy for themselves.
        if (!TryParse(current, out var currentVersion) || !TryParse(latest, out var latestVersion))
            return false;

        return latestVersion > currentVersion;
    }

    private static bool TryParse(string value, out Version version)
    {
        // Tolerates a leading "v" and any build metadata the version string may carry.
        var trimmed = value.Trim().TrimStart('v', 'V');
        var cut = trimmed.IndexOfAny(['-', '+', ' ']);
        if (cut >= 0) trimmed = trimmed[..cut];

        return Version.TryParse(trimmed, out version!);
    }
}
