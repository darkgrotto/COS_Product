using CountOrSell.Api.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CountOrSell.Tests.Integration.Auth;

public class OAuthConfigurationTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Theory]
    [InlineData("google")]
    [InlineData("microsoft")]
    [InlineData("github")]
    public void Provider_IsNotConfigured_WhenSecretsAbsent(string provider)
    {
        var config = BuildConfig(new Dictionary<string, string?>()); // empty config
        var service = new OAuthConfigService(config);

        Assert.False(service.IsConfigured(provider));
    }

    [Theory]
    [InlineData("google")]
    [InlineData("microsoft")]
    [InlineData("github")]
    public void Provider_IsNotConfigured_WhenOnlyClientIdPresent(string provider)
    {
        var key = provider switch
        {
            "google" => "OAuth:Google:ClientId",
            "microsoft" => "OAuth:Microsoft:ClientId",
            "github" => "OAuth:GitHub:ClientId",
            _ => throw new ArgumentException(provider)
        };
        var config = BuildConfig(new Dictionary<string, string?> { [key] = "some-client-id" });
        var service = new OAuthConfigService(config);

        Assert.False(service.IsConfigured(provider));
    }

    [Theory]
    [InlineData("google", "OAuth:Google:ClientId", "OAuth:Google:ClientSecret")]
    [InlineData("microsoft", "OAuth:Microsoft:ClientId", "OAuth:Microsoft:ClientSecret")]
    [InlineData("github", "OAuth:GitHub:ClientId", "OAuth:GitHub:ClientSecret")]
    public void Provider_IsConfigured_WhenBothSecretsPresent(string provider, string idKey, string secretKey)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            [idKey] = "client-id",
            [secretKey] = "client-secret"
        });
        var service = new OAuthConfigService(config);

        Assert.True(service.IsConfigured(provider));
    }

    [Fact]
    public void UnknownProvider_IsNeverConfigured()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["OAuth:SomeProvider:ClientId"] = "id",
            ["OAuth:SomeProvider:ClientSecret"] = "secret"
        });
        var service = new OAuthConfigService(config);

        Assert.False(service.IsConfigured("someprovider"));
    }
}
