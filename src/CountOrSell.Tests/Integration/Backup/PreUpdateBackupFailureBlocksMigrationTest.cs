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

public class PreUpdateBackupFailureBlocksMigrationTest
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PreUpdateBackupFailureBlocksMigrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SchemaApproval_BlockedWhenPreUpdateBackupFails()
    {
        PendingSchemaUpdate? seededUpdate = null;

        var factory = _factory.WithWebHostBuilder(builder =>
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

                var dbName = $"SchemaApprovalTestDb_{Guid.NewGuid()}";
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase(dbName));

                // Replace IUpdateCheckTrigger with no-op
                var triggerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUpdateCheckTrigger));
                if (triggerDescriptor != null) services.Remove(triggerDescriptor);
                var mockTrigger = new Mock<IUpdateCheckTrigger>();
                mockTrigger.Setup(t => t.TriggerAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UpdateCheckResult(false, "No packages available."));
                services.AddSingleton(mockTrigger.Object);

                // Replace IPreUpdateBackupService with a failing mock
                var backupDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IPreUpdateBackupService));
                if (backupDescriptor != null) services.Remove(backupDescriptor);
                var mockBackup = new Mock<IPreUpdateBackupService>();
                mockBackup.Setup(b => b.TakeBackupAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                services.AddScoped<IPreUpdateBackupService>(_ => mockBackup.Object);

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, SchemaApprovalTestAuthHandler>(
                        "Test", options => { });
            });
        });

        // Seed a pending schema update
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            seededUpdate = new PendingSchemaUpdate
            {
                SchemaVersion = "99",
                Description = "Test schema update",
                DownloadUrl = "https://countorsell.com/updates/schema-99.zip",
                ZipSha256 = "abc123",
                DetectedAt = DateTime.UtcNow
            };
            db.PendingSchemaUpdates.Add(seededUpdate);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test");

        var response = await client.PostAsync(
            $"/api/updates/schema/{seededUpdate.Id}/approve", null);

        // Should return 422 because backup failed
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        // Verify the schema update was NOT approved in DB
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updated = await db.PendingSchemaUpdates.FindAsync(seededUpdate.Id);
            Assert.NotNull(updated);
            Assert.False(updated!.IsApproved);
        }
    }
}

public class SchemaApprovalTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SchemaApprovalTestAuthHandler(
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
