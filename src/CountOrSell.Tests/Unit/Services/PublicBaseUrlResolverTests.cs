using CountOrSell.Api.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

public class PublicBaseUrlResolverTests
{
    [Fact]
    public void Falls_Back_To_Request_Origin_When_Unset()
    {
        var r = PublicBaseUrlResolver.Resolve(configuredValue: null, fallbackBaseUrl: "https://host.example/");
        Assert.True(r.Success);
        Assert.Equal("https://host.example", r.BaseUrl);
        Assert.Null(r.Error);
    }

    [Fact]
    public void Falls_Back_When_Configured_Value_Is_Whitespace()
    {
        var r = PublicBaseUrlResolver.Resolve(configuredValue: "   ", fallbackBaseUrl: "https://host.example");
        Assert.True(r.Success);
        Assert.Equal("https://host.example", r.BaseUrl);
    }

    [Fact]
    public void Uses_Configured_Value_And_Trims_Trailing_Slash()
    {
        var r = PublicBaseUrlResolver.Resolve(
            configuredValue: "https://canonical.example/",
            fallbackBaseUrl: "https://attacker.example");
        Assert.True(r.Success);
        Assert.Equal("https://canonical.example", r.BaseUrl);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    [InlineData("//no-scheme.example")]
    public void Rejects_Non_Http_Or_Malformed_Configured_Value(string bad)
    {
        var r = PublicBaseUrlResolver.Resolve(configuredValue: bad, fallbackBaseUrl: "https://host.example");
        Assert.False(r.Success);
        Assert.Null(r.BaseUrl);
        Assert.NotNull(r.Error);
    }

    [Fact]
    public void Configured_Value_Overrides_Fallback_Even_When_Fallback_Is_Different_Host()
    {
        // The whole point of this fix: an attacker-supplied Host header (= fallback) must NOT
        // be able to override the admin's configured PUBLIC_BASE_URL.
        var r = PublicBaseUrlResolver.Resolve(
            configuredValue: "https://canonical.example",
            fallbackBaseUrl: "https://attacker.example");
        Assert.Equal("https://canonical.example", r.BaseUrl);
    }
}
