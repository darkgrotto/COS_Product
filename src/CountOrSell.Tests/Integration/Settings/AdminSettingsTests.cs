using CountOrSell.Api.Controllers;
using CountOrSell.Data;
using CountOrSell.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CountOrSell.Tests.Integration.Settings;

[Trait("Category", "RequiresDocker")]
public class AdminSettingsTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public AdminSettingsTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static SettingsController BuildController(AppDbContext db) =>
        new(db, new ConfigurationBuilder().Build());

    // --- Instance name ---

    [Fact]
    public async Task GetInstanceSettings_ReturnsEmptyByDefault()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.GetInstanceSettings(CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        var value = result.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task UpdateInstanceSettings_PersistsAndReturnsNewName()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var saveResult = await controller.UpdateInstanceSettings(
            new InstanceSettingsRequest { InstanceName = "My Test Instance" },
            CancellationToken.None);
        Assert.IsType<OkResult>(saveResult);

        var getResult = await controller.GetInstanceSettings(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(getResult);
    }

    [Fact]
    public async Task UpdateInstanceSettings_RejectsBlankName()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.UpdateInstanceSettings(
            new InstanceSettingsRequest { InstanceName = "   " },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- TCGPlayer key ---

    [Fact]
    public async Task GetTcgPlayerSettings_NotConfiguredByDefault()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.GetTcgPlayerSettings(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetTcgPlayerKey_MasksKeyOnRead()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        await controller.SetTcgPlayerKey(
            new TcgPlayerKeyRequest { ApiKey = "ABCDEF1234567890" },
            CancellationToken.None);

        var result = await controller.GetTcgPlayerSettings(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetTcgPlayerKey_RejectsBlankKey()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.SetTcgPlayerKey(
            new TcgPlayerKeyRequest { ApiKey = "" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ClearTcgPlayerKey_RemovesKey()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        await controller.SetTcgPlayerKey(
            new TcgPlayerKeyRequest { ApiKey = "somekey12345" },
            CancellationToken.None);

        var clearResult = await controller.ClearTcgPlayerKey(CancellationToken.None);
        Assert.IsType<OkResult>(clearResult);
    }

    // --- Self-enrollment ---

    [Fact]
    public async Task GetSelfEnrollment_DisabledByDefault()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.GetSelfEnrollment(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateSelfEnrollment_TogglesState()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var enableResult = await controller.UpdateSelfEnrollment(
            new SelfEnrollmentRequest { Enabled = true },
            CancellationToken.None);
        Assert.IsType<OkResult>(enableResult);

        var getResult = await controller.GetSelfEnrollment(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(getResult);

        var disableResult = await controller.UpdateSelfEnrollment(
            new SelfEnrollmentRequest { Enabled = false },
            CancellationToken.None);
        Assert.IsType<OkResult>(disableResult);
    }

    // --- OAuth ---

    [Fact]
    public async Task GetOAuthSettings_ReturnsThreeProviders()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.GetOAuthSettings(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateOAuthProvider_PersistsClientId()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.UpdateOAuthProvider(
            "github",
            new OAuthProviderRequest { ClientId = "gh_test_client_id", ClientSecret = "gh_secret" },
            CancellationToken.None);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task UpdateOAuthProvider_RejectsUnknownProvider()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        var result = await controller.UpdateOAuthProvider(
            "notreal",
            new OAuthProviderRequest { ClientId = "id", ClientSecret = "secret" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ClearOAuthProvider_RemovesKeys()
    {
        await using var db = _fixture.CreateContext();
        var controller = BuildController(db);

        await controller.UpdateOAuthProvider(
            "google",
            new OAuthProviderRequest { ClientId = "goog_id", ClientSecret = "goog_secret" },
            CancellationToken.None);

        var clearResult = await controller.ClearOAuthProvider("google", CancellationToken.None);
        Assert.IsType<OkResult>(clearResult);
    }
}
