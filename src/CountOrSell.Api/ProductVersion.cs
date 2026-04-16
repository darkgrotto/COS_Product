using System.Reflection;

namespace CountOrSell.Api;

public static class ProductVersion
{
    // Semver-only string used for version comparisons (e.g. "1.1.0").
    public static readonly string Current;

    // Display string including git hash when available (e.g. "1.1.0 (abc1234)").
    public static readonly string Display;

    static ProductVersion()
    {
        var raw = typeof(ProductVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";

        var plusIdx = raw.IndexOf('+');
        if (plusIdx >= 0)
        {
            Current = raw[..plusIdx];
            var hash = raw[(plusIdx + 1)..];
            if (hash.Length > 7) hash = hash[..7];
            Display = $"{Current} ({hash})";
        }
        else
        {
            Current = raw;
            Display = raw;
        }
    }
}
