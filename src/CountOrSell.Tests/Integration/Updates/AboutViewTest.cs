using System.Net;
using System.Net.Http.Headers;
using CountOrSell.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

public class AboutViewTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AboutViewTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(string role)
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
                    opt.UseInMemoryDatabase($"AboutTestDb_{Guid.NewGuid()}"));

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
                services.Configure<TestAuthOptions>(opt => opt.Role = role);
            });
        });
    }

    [Fact]
    public async Task About_AsAdmin_Returns200WithExpectedFields()
    {
        var factory = BuildFactory("Admin");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync("/api/about");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("currentVersion", body);
        Assert.Contains("latestReleasedVersion", body);
        Assert.Contains("updatePending", body);
        Assert.Contains("instanceName", body);
    }

    [Fact]
    public async Task About_AsGeneralUser_Returns200WithExpectedFields()
    {
        var factory = BuildFactory("GeneralUser");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync("/api/about");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("currentVersion", body);
        Assert.Contains("latestReleasedVersion", body);
    }

    [Fact]
    public async Task About_Unauthenticated_Returns401()
    {
        var factory = BuildFactory("Admin");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        // No Authorization header

        var response = await client.GetAsync("/api/about");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
