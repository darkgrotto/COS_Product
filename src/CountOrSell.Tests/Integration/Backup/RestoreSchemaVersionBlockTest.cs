using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CountOrSell.Api.Background.Updates;
using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.Backup;

public class RestoreSchemaVersionBlockTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RestoreSchemaVersionBlockTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory()
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
                    opt.UseInMemoryDatabase($"RestoreTestDb_{Guid.NewGuid()}"));

                // Replace IUpdateCheckTrigger with no-op
                var triggerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUpdateCheckTrigger));
                if (triggerDescriptor != null) services.Remove(triggerDescriptor);
                var mockTrigger = new Mock<IUpdateCheckTrigger>();
                mockTrigger.Setup(t => t.TriggerAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                services.AddSingleton(mockTrigger.Object);

                // Replace IProcessRunner with a no-op for tests
                var runnerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IProcessRunner));
                if (runnerDescriptor != null) services.Remove(runnerDescriptor);
                var mockRunner = new Mock<IProcessRunner>();
                mockRunner.Setup(r => r.RunAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync("-- mock output");
                services.AddScoped<IProcessRunner>(_ => mockRunner.Object);

                // Add test authentication scheme
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, RestoreTestAuthHandler>(
                        "Test", options => { });
            });
        });
    }

    [Fact]
    public async Task Restore_Returns409_WhenBackupSchemaVersionExceedsDeployment()
    {
        var archiveBytes = BuildFakeBackupArchive(schemaVersion: 9999);
        var factory = BuildFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(archiveBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "backup.zip");

        var response = await client.PostAsync("/api/restore", content);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Restore_Returns200_WhenBackupSchemaVersionMatchesDeployment()
    {
        // Schema version 1 matches ApplicationSchemaVersion = 1
        var archiveBytes = BuildFakeBackupArchive(schemaVersion: 1);
        var factory = BuildFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(archiveBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "backup.zip");

        var response = await client.PostAsync("/api/restore", content);

        // psql is mocked via IProcessRunner so restore completes without a real DB
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static byte[] BuildFakeBackupArchive(int schemaVersion)
    {
        var metadata = new BackupMetadata
        {
            BackupFormatVersion = 1,
            InstanceName = "test",
            SchemaVersion = schemaVersion,
            BackupType = "scheduled",
            Timestamp = DateTime.UtcNow,
            Label = "test"
        };

        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("metadata.json");
            using (var s = entry.Open())
            {
                s.Write(System.Text.Encoding.UTF8.GetBytes(
                    System.Text.Json.JsonSerializer.Serialize(metadata)));
            }

            var dump = archive.CreateEntry("dump.sql");
            using (var ds = dump.Open())
            {
                ds.Write(System.Text.Encoding.UTF8.GetBytes("-- empty"));
            }
        }
        ms.Position = 0;
        return ms.ToArray();
    }
}

public class RestoreTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RestoreTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "testadmin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
