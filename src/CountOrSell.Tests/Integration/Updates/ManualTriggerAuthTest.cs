using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

public class ManualTriggerAuthTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ManualTriggerAuthTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(string role)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);
                var dbCtxDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AppDbContext));
                if (dbCtxDescriptor != null) services.Remove(dbCtxDescriptor);
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase($"AuthTestDb_{Guid.NewGuid()}"));

                // Replace UpdateCheckService / IUpdateCheckTrigger with no-op mock
                var triggerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUpdateCheckTrigger));
                if (triggerDescriptor != null) services.Remove(triggerDescriptor);

                var mockTrigger = new Mock<IUpdateCheckTrigger>();
                mockTrigger.Setup(t => t.TriggerAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UpdateCheckResult(false, "No packages available."));
                services.AddSingleton(mockTrigger.Object);

                // Add test authentication scheme
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
                services.Configure<TestAuthOptions>(opt => opt.Role = role);
            });
        });
    }

    [Fact]
    public async Task ManualCheck_AsAdmin_Returns200()
    {
        var factory = BuildFactory("Admin");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.PostAsync("/api/updates/check", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ManualCheck_AsGeneralUser_Returns403()
    {
        var factory = BuildFactory("GeneralUser");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.PostAsync("/api/updates/check", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ManualCheck_Unauthenticated_Returns401()
    {
        var factory = BuildFactory("Admin");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        // No auth header

        var response = await client.PostAsync("/api/updates/check", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// Options for the test auth handler
public class TestAuthOptions : AuthenticationSchemeOptions
{
    public string Role { get; set; } = "Admin";
}

// Simple test auth handler that creates a principal with the configured role
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<TestAuthOptions> _testOptions;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<TestAuthOptions> testOptions)
        : base(options, logger, encoder)
    {
        _testOptions = testOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = _testOptions.CurrentValue.Role;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
