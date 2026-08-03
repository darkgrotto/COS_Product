namespace CountOrSell.Api.Services;

// Update content is only ever served from countorsell.com. The top-level manifest is
// fetched from a hardcoded URL, but the per-package manifest and download URLs are read
// from that (unsigned) manifest body, so they are validated against this allowlist before
// any outbound request. This prevents a poisoned/compromised manifest from redirecting the
// server to fetch arbitrary internal hosts (SSRF - e.g. cloud metadata endpoints).
internal static class UpdateSource
{
    private const string AllowedHost = "www.countorsell.com";

    public static bool IsAllowed(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, AllowedHost, StringComparison.OrdinalIgnoreCase);
}
