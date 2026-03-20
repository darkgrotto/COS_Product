using CountOrSell.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CountOrSell.Tests.Unit.Demo;

public class DemoStatusTests
{
    private static DemoModeService Build(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        return new DemoModeService(configuration);
    }

    [Fact]
    public void IsDemo_False_WhenDemoModeNotSet()
    {
        var service = Build(new Dictionary<string, string?> { });
        Assert.False(service.IsDemo);
    }

    [Fact]
    public void IsDemo_False_WhenDemoModeFalse()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "false" });
        Assert.False(service.IsDemo);
    }

    [Fact]
    public void IsDemo_True_WhenDemoModeTrue()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "true" });
        Assert.True(service.IsDemo);
    }

    [Fact]
    public void IsDemo_True_CaseInsensitive()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "TRUE" });
        Assert.True(service.IsDemo);
    }

    [Fact]
    public void DemoSets_Empty_WhenNotDemo()
    {
        var service = Build(new Dictionary<string, string?> { });
        Assert.Empty(service.DemoSets);
    }

    [Fact]
    public void DemoSets_NotEmpty_WhenDemo()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "true" });
        Assert.NotEmpty(service.DemoSets);
    }

    [Fact]
    public void DemoSets_AllLowercase()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "true" });
        foreach (var set in service.DemoSets)
            Assert.Equal(set, set.ToLowerInvariant());
    }

    [Fact]
    public void SecondsRemaining_Zero_WhenNotDemo()
    {
        var service = Build(new Dictionary<string, string?> { });
        Assert.Equal(0, service.SecondsRemaining);
    }

    [Fact]
    public void SecondsRemaining_Zero_WhenNoExpiresAt()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "true" });
        Assert.Equal(0, service.SecondsRemaining);
    }

    [Fact]
    public void SecondsRemaining_Positive_WhenFutureExpiry()
    {
        var future = DateTimeOffset.UtcNow.AddHours(1).ToString("o");
        var service = Build(new Dictionary<string, string?>
        {
            ["DEMO_MODE"] = "true",
            ["DEMO_EXPIRES_AT"] = future,
        });
        Assert.True(service.SecondsRemaining > 0);
    }

    [Fact]
    public void SecondsRemaining_Zero_NotNegative_WhenExpired()
    {
        var past = DateTimeOffset.UtcNow.AddHours(-1).ToString("o");
        var service = Build(new Dictionary<string, string?>
        {
            ["DEMO_MODE"] = "true",
            ["DEMO_EXPIRES_AT"] = past,
        });
        Assert.Equal(0, service.SecondsRemaining);
    }

    [Fact]
    public void ExpiresAt_Null_WhenNoEnvVar()
    {
        var service = Build(new Dictionary<string, string?> { ["DEMO_MODE"] = "true" });
        Assert.Null(service.ExpiresAt);
    }

    [Fact]
    public void ExpiresAt_Parsed_WhenValid()
    {
        var future = DateTimeOffset.UtcNow.AddHours(2);
        var service = Build(new Dictionary<string, string?>
        {
            ["DEMO_MODE"] = "true",
            ["DEMO_EXPIRES_AT"] = future.ToString("o"),
        });
        Assert.NotNull(service.ExpiresAt);
        Assert.True(Math.Abs((service.ExpiresAt!.Value - future).TotalSeconds) < 2);
    }
}
