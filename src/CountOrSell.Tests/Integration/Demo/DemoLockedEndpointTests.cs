using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Data;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.Demo;

public class DemoLockedEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoLockedEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildDemoFactory(string role = "Admin")
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
                    opt.UseInMemoryDatabase($"DemoLockedDb_{Guid.NewGuid()}"));

                var triggerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUpdateCheckTrigger));
                if (triggerDescriptor != null) services.Remove(triggerDescriptor);
                var mockTrigger = new Mock<IUpdateCheckTrigger>();
                mockTrigger.Setup(t => t.TriggerAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UpdateCheckResult(false, "No packages available."));
                services.AddSingleton(mockTrigger.Object);

                // Replace IDemoModeService with a mock that always reports demo active.
                // This avoids relying on configuration propagation and ensures the
                // DemoLockedFilter sees IsDemo = true for every test case.
                var demoDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDemoModeService));
                if (demoDescriptor != null) services.Remove(demoDescriptor);
                var mockDemo = new Mock<IDemoModeService>();
                mockDemo.Setup(d => d.IsDemo).Returns(true);
                mockDemo.Setup(d => d.DemoSets).Returns(Array.Empty<string>());
                mockDemo.Setup(d => d.ExpiresAt).Returns((DateTimeOffset?)null);
                mockDemo.Setup(d => d.SecondsRemaining).Returns(0);
                services.AddSingleton(mockDemo.Object);

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DemoTestAuthHandler>(
                        "Test", _ => { });
                services.Configure<DemoTestAuthOptions>(opt => opt.Role = role);
            });
        });
    }

    private static HttpClient CreateAuthClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    public static IEnumerable<object[]> LockedEndpoints =>
        new List<object[]>
        {
            new object[] { HttpMethod.Post, "/api/collection/refresh-price/eoe001" },
            new object[] { HttpMethod.Get, "/api/wishlist/export/tcgplayer" },
            new object[] { HttpMethod.Post, "/api/backup/trigger" },
            new object[] { HttpMethod.Post, "/api/restore" },
            new object[] { HttpMethod.Post, $"/api/restore/{Guid.NewGuid()}" },
            new object[] { HttpMethod.Post, "/api/backup/destinations" },
            new object[] { HttpMethod.Delete, $"/api/backup/destinations/{Guid.NewGuid()}" },
            new object[] { HttpMethod.Patch, "/api/settings/instance" },
            new object[] { HttpMethod.Patch, "/api/settings/oauth/google" },
            new object[] { HttpMethod.Delete, "/api/settings/oauth/google" },
            new object[] { HttpMethod.Patch, "/api/settings/self-enrollment" },
            new object[] { HttpMethod.Post, "/api/updates/check" },
            new object[] { HttpMethod.Post, $"/api/updates/schema/1/approve" },
            new object[] { HttpMethod.Post, $"/api/users/{Guid.NewGuid()}/remove" },
        };

    [Theory]
    [MemberData(nameof(LockedEndpoints))]
    public async Task DemoLockedEndpoint_Returns403_InDemoMode(HttpMethod method, string url)
    {
        var factory = BuildDemoFactory("Admin");
        var client = CreateAuthClient(factory);

        HttpContent? content;
        if (url == "/api/restore")
        {
            // Restore endpoint expects multipart/form-data with an IFormFile named "file".
            // Send a minimal zip-named file so model binding succeeds and the
            // DemoLockedFilter runs before the action body is reached.
            var form = new MultipartFormDataContent();
            var fileBytes = new ByteArrayContent(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
            fileBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
            form.Add(fileBytes, "file", "backup.zip");
            content = form;
        }
        else if (method == HttpMethod.Post || method == HttpMethod.Patch)
        {
            content = new StringContent("{}", Encoding.UTF8, "application/json");
        }
        else
        {
            content = null;
        }

        var request = new HttpRequestMessage(method, url);
        if (content != null) request.Content = content;

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("demo mode", body, StringComparison.OrdinalIgnoreCase);
    }
}

public class DemoTestAuthOptions : AuthenticationSchemeOptions
{
    public string Role { get; set; } = "Admin";
}

public class DemoTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<DemoTestAuthOptions> _testOptions;

    public DemoTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<DemoTestAuthOptions> testOptions)
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
            new Claim(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
