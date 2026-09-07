namespace CountOrSell.Api.Services;

// Update content is only ever served from countorsell.com. The top-level manifest is fetched
// from a hardcoded URL, but the per-package manifest and download URLs are read from that
// (unsigned) manifest body, so every one of them is resolved through this allowlist before any
// outbound request. This prevents a poisoned/compromised manifest from redirecting the server
// to fetch arbitrary internal hosts (SSRF - e.g. cloud metadata endpoints).
//
// The allowlist is fixed in source. That is the point - it is an allowlist, not a configurable
// update source - but it does mean a host the Backend publishes from can only change with a
// Product release, so the hosts are named rather than pattern-matched and kept to a minimum.
internal static class UpdateSource
{
    // The website, which serves the top-level manifest and the signing JWKS.
    private const string SiteHost = "www.countorsell.com";

    // The apex is not a routed hostname at the CDN edge (it answers Cloudflare error 1016),
    // so an apex URL is accepted but rewritten to the www host before it is fetched.
    private const string ApexHost = "countorsell.com";

    // Stable hostname for published packages (package.zip, per-package manifest.json and its
    // .sig, and image blobs). Fronts the Backend's object storage, so the storage account
    // behind it can move without a Product release - which is the whole reason it exists.
    // The storage account host it replaced was allowlisted transitionally and is no longer
    // accepted: every manifest entry has been served through this hostname since 2026-09-07.
    private const string PackageHost = "packages.countorsell.com";

    private static readonly string[] AllowedHosts = [SiteHost, PackageHost];

    // The one URL the update system starts from. Hardcoded on purpose - the update source is
    // not configurable - and kept here so the allowed host is stated in exactly one place.
    public const string ManifestUrl = "https://" + SiteHost + "/updates/manifest.json";

    // Validates an untrusted URL taken from the website manifest and yields the form that
    // should actually be requested. Returns false - and resolves nothing - for any URL that is
    // not https on an allowed host.
    public static bool TryResolve(string? url, out string resolved)
    {
        resolved = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;

        // Apex is the only host that is rewritten. Package hosts are never swapped for one
        // another: they are distinct origins, and a CNAME's target can require its own Host.
        if (string.Equals(uri.Host, ApexHost, StringComparison.OrdinalIgnoreCase))
        {
            resolved = new UriBuilder(uri) { Host = SiteHost }.Uri.AbsoluteUri;
            return true;
        }

        foreach (var host in AllowedHosts)
        {
            if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
            {
                resolved = url!;
                return true;
            }
        }

        return false;
    }

    // Directory portion of an already-resolved package manifest URL - the base that every
    // per-file image fetch is appended to. Taking it from the resolved URL rather than from
    // the raw manifest field is what keeps image fetches on the allowed source too.
    public static string BaseUrlOf(string resolvedManifestUrl)
    {
        var uri = new Uri(resolvedManifestUrl, UriKind.Absolute);
        var path = uri.AbsolutePath;
        var lastSlash = path.LastIndexOf('/');
        return uri.GetLeftPart(UriPartial.Authority) + (lastSlash >= 0 ? path[..(lastSlash + 1)] : "/");
    }
}
