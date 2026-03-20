using System.Net;
using CountOrSell.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Demo;

public class DemoModeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoModeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(bool demoEnabled = false)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);
                var dbCtxDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AppDbContext));
                if (dbCtxDescriptor != null) services.Remove(dbCtxDescriptor);
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase($"DemoModeTestDb_{Guid.NewGuid()}"));
            });

            if (demoEnabled)
            {
                builder.UseSetting("DEMO_MODE", "true");
                builder.UseSetting("DEMO_EXPIRES_AT",
                    DateTimeOffset.UtcNow.AddHours(1).ToString("o"));
            }
        });
    }

    [Fact]
    public async Task GetStatus_Returns404_WhenDemoModeOff()
    {
        var factory = BuildFactory(demoEnabled: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/demo/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_Returns200_WhenDemoModeOn()
    {
        var factory = BuildFactory(demoEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/demo/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_IncludesDemoSets_WhenDemoModeOn()
    {
        var factory = BuildFactory(demoEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/demo/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("demoSets", body);
        Assert.Contains("eoe", body);
    }

    [Fact]
    public async Task GetStatus_IncludesPositiveSecondsRemaining_WhenFutureExpiry()
    {
        var factory = BuildFactory(demoEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/demo/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("secondsRemaining", body);
        Assert.DoesNotContain("\"secondsRemaining\":0", body);
    }
}
