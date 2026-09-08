using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// "Update available" must mean the release is newer, not merely different.
public class AppVersionComparisonTests
{
    [Theory]
    [InlineData("1.2.0", "1.2.1")]
    [InlineData("1.2.1", "1.3.0")]
    [InlineData("1.9.0", "1.10.0")]   // string comparison sorts 1.10.0 below 1.9.0
    [InlineData("1.2.1", "2.0.0")]
    public void Reports_An_Update_When_The_Release_Is_Newer(string current, string latest)
        => Assert.True(AppVersionComparison.IsUpdateAvailable(current, latest));

    [Theory]
    // The reported case: an instance on 1.2.1 whose cached "latest" is still 1.2.0,
    // because the release check was rate limited. It advertised an update to an older
    // version.
    [InlineData("1.2.1", "1.2.0")]
    [InlineData("1.10.0", "1.9.0")]
    [InlineData("2.0.0", "1.9.9")]
    public void Reports_No_Update_When_The_Instance_Is_Ahead(string current, string latest)
        => Assert.False(AppVersionComparison.IsUpdateAvailable(current, latest));

    [Fact]
    public void Reports_No_Update_When_The_Versions_Match()
        => Assert.False(AppVersionComparison.IsUpdateAvailable("1.2.1", "1.2.1"));

    [Theory]
    [InlineData("1.2.1", "v1.2.2")]
    [InlineData("v1.2.1", "1.2.2")]
    public void Tolerates_A_Leading_V(string current, string latest)
        => Assert.True(AppVersionComparison.IsUpdateAvailable(current, latest));

    [Fact]
    public void Ignores_Build_Metadata_On_Either_Side()
    {
        // ProductVersion.Display carries a git hash; the semver-only Current should not,
        // but tolerating it costs nothing and avoids a false positive if it ever does.
        Assert.False(AppVersionComparison.IsUpdateAvailable("1.2.1+abc1234", "1.2.1"));
        Assert.True(AppVersionComparison.IsUpdateAvailable("1.2.1+abc1234", "1.2.2"));
    }

    [Theory]
    [InlineData(null, "1.2.1")]
    [InlineData("1.2.1", null)]
    [InlineData("1.2.1", "")]
    [InlineData("1.2.1", "   ")]
    public void Reports_No_Update_Without_Both_Versions(string? current, string? latest)
        => Assert.False(AppVersionComparison.IsUpdateAvailable(current, latest));

    [Theory]
    [InlineData("not-a-version", "1.2.1")]
    [InlineData("1.2.1", "not-a-version")]
    public void Stays_Quiet_Rather_Than_Guessing_On_An_Unparseable_Version(string current, string latest)
    {
        // Claiming an update we cannot substantiate is worse than saying nothing; the
        // About view still shows both strings.
        Assert.False(AppVersionComparison.IsUpdateAvailable(current, latest));
    }
}
