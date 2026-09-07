using System.Reflection;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// UpdateSource is the SSRF boundary for the update system: the per-package manifest and
// download URLs come from the website manifest, which is unsigned, so anything that reaches
// an outbound request must first survive TryResolve.
public class UpdateSourceTests
{
    private static readonly Type SourceType =
        Assembly.Load("CountOrSell.Api").GetType("CountOrSell.Api.Services.UpdateSource")
        ?? throw new InvalidOperationException("UpdateSource type not found");

    private static bool TryResolve(string? url, out string resolved)
    {
        var args = new object?[] { url, null };
        var ok = (bool)SourceType.GetMethod("TryResolve")!.Invoke(null, args)!;
        resolved = (string)args[1]!;
        return ok;
    }

    private static string BaseUrlOf(string url) =>
        (string)SourceType.GetMethod("BaseUrlOf")!.Invoke(null, new object[] { url })!;

    private static string ManifestUrl =>
        (string)SourceType.GetField("ManifestUrl")!.GetValue(null)!;

    [Fact]
    public void ManifestUrl_Uses_The_Www_Host()
    {
        // The apex answers Cloudflare 1016, so the hardcoded entry point must be www.
        Assert.Equal("https://www.countorsell.com/updates/manifest.json", ManifestUrl);
    }

    [Theory]
    // The website itself.
    [InlineData("https://www.countorsell.com/updates/manifest.json")]
    // The stable package hostname every manifest entry is served through.
    [InlineData("https://packages.countorsell.com/publish-a/20260513-190155-32fb03/manifest.json")]
    [InlineData("https://packages.countorsell.com/publish-a/20260513-190155-32fb03/package.zip")]
    public void Allows_The_Canonical_Update_Hosts_Unchanged(string url)
    {
        Assert.True(TryResolve(url, out var resolved));
        Assert.Equal(url, resolved);
    }

    [Theory]
    [InlineData(
        "https://countorsell.com/updates/manifest.json",
        "https://www.countorsell.com/updates/manifest.json")]
    [InlineData(
        "https://COUNTORSELL.COM/updates/package.zip",
        "https://www.countorsell.com/updates/package.zip")]
    public void Rewrites_The_Apex_Host_To_Www(string url, string expected)
    {
        Assert.True(TryResolve(url, out var resolved));
        Assert.Equal(expected, resolved);
    }

    [Theory]
    // Cloud metadata endpoints - the reason this allowlist exists.
    [InlineData("https://169.254.169.254/latest/meta-data/iam/security-credentials/")]
    [InlineData("https://metadata.google.internal/computeMetadata/v1/")]
    // Other internal targets.
    [InlineData("https://localhost/updates/manifest.json")]
    [InlineData("https://127.0.0.1:5432/")]
    [InlineData("https://postgres:5432/")]
    // Lookalike hosts: suffix, prefix, subdomain and homograph-ish variations.
    [InlineData("https://evil-countorsell.com/updates/manifest.json")]
    [InlineData("https://countorsell.com.evil.test/updates/manifest.json")]
    [InlineData("https://www.countorsell.com.evil.test/updates/manifest.json")]
    [InlineData("https://updates.countorsell.com/manifest.json")]
    [InlineData("https://packages.countorsell.com.evil.test/publish-a/manifest.json")]
    [InlineData("https://evil-packages.countorsell.com.evil.test/publish-a/manifest.json")]
    [InlineData("https://wwwcountorsell.com/updates/manifest.json")]
    // Azure blob storage generally, including the account packages used to be served from
    // directly - allowlisted transitionally until the manifest moved to the stable hostname,
    // and deliberately not accepted any more. Nothing pins a storage account name now.
    [InlineData("https://cosadminstoreprod.blob.core.windows.net/publish-a/20260513-190155-32fb03/manifest.json")]
    [InlineData("https://cosadminstoreprod.blob.core.windows.net/publish-b/20260419-163118-265cfc/package.zip")]
    [InlineData("https://evilstore.blob.core.windows.net/publish-a/manifest.json")]
    [InlineData("https://cosadminstoreprod.blob.core.windows.net.evil.test/manifest.json")]
    // Non-https schemes, including ones that are not network fetches at all.
    [InlineData("http://www.countorsell.com/updates/manifest.json")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://www.countorsell.com/updates/manifest.json")]
    // Not absolute URLs.
    [InlineData("/updates/manifest.json")]
    [InlineData("www.countorsell.com/updates/manifest.json")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_Anything_Off_The_Allowed_Source(string? url)
    {
        Assert.False(TryResolve(url, out var resolved));
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void Does_Not_Rewrite_The_Package_Host()
    {
        // Only the apex is ever rewritten. A package URL is fetched from the host it was
        // published on - a fronting hostname's origin can require its own Host header, so
        // substituting one host for another would break the request.
        const string packages = "https://packages.countorsell.com/publish-a/pkg/manifest.json";
        Assert.True(TryResolve(packages, out var resolved));
        Assert.Equal(packages, resolved);
    }

    [Fact]
    public void Rejects_A_Host_Smuggled_Through_Userinfo()
    {
        // Uri.Host here is evil.test, not www.countorsell.com - the request would go to the
        // attacker's host with the allowed host presented only as a credential.
        Assert.False(TryResolve("https://www.countorsell.com@evil.test/updates/manifest.json", out _));
    }

    [Theory]
    [InlineData(
        "https://www.countorsell.com/updates/manifest.json",
        "https://www.countorsell.com/updates/")]
    [InlineData(
        "https://packages.countorsell.com/publish-a/20260513-190155-32fb03/manifest.json",
        "https://packages.countorsell.com/publish-a/20260513-190155-32fb03/")]
    [InlineData(
        "https://www.countorsell.com/manifest.json",
        "https://www.countorsell.com/")]
    public void BaseUrlOf_Returns_The_Directory_Of_The_Manifest(string url, string expected)
    {
        Assert.Equal(expected, BaseUrlOf(url));
    }

    [Fact]
    public void BaseUrlOf_Drops_Query_And_Fragment()
    {
        // Image paths are appended to this base, so anything after the path must not survive.
        Assert.Equal(
            "https://packages.countorsell.com/publish-a/pkg/",
            BaseUrlOf("https://packages.countorsell.com/publish-a/pkg/manifest.json?sv=token#frag"));
    }
}
