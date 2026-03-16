using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CountOrSell.Tests.Integration.Collection;

/// <summary>
/// Auth handler that reads userId and role from request headers so
/// tests running across WebApplicationFactory threads work correctly.
/// Header X-Test-User-Id carries the Guid; X-Test-User-Role carries the role.
/// </summary>
public class HeaderDrivenAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public HeaderDrivenAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User-Id", out var userIdVal))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Guid.TryParse(userIdVal, out var parsed) ? parsed : Guid.NewGuid();
        var role = Request.Headers.TryGetValue("X-Test-User-Role", out var roleVal)
            ? roleVal.ToString()
            : "GeneralUser";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, $"user-{userId}"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "HeaderDriven");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "HeaderDriven");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class CrossUserAccessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly Guid UserAId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserBId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    public CrossUserAccessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(string dbName)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var desc = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (desc != null) services.Remove(desc);
                var ctxDesc = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AppDbContext));
                if (ctxDesc != null) services.Remove(ctxDesc);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseInMemoryDatabase(dbName));

                services.AddAuthentication("HeaderDriven")
                    .AddScheme<AuthenticationSchemeOptions, HeaderDrivenAuthHandler>(
                        "HeaderDriven", _ => { });
            });
        });
    }

    private static HttpClient ClientAs(WebApplicationFactory<Program> factory, Guid userId, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-User-Role", role);
        return client;
    }

    private static async Task SeedAsync(WebApplicationFactory<Program> factory, Guid entryId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.AddRange(
            new User
            {
                Id = UserAId, Username = "usera", PasswordHash = "x",
                AuthType = AuthType.Local, Role = UserRole.GeneralUser,
                IsBuiltinAdmin = false, State = AccountState.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = UserBId, Username = "userb", PasswordHash = "x",
                AuthType = AuthType.Local, Role = UserRole.GeneralUser,
                IsBuiltinAdmin = false, State = AccountState.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = AdminId, Username = "admin", PasswordHash = "x",
                AuthType = AuthType.Local, Role = UserRole.Admin,
                IsBuiltinAdmin = true, State = AccountState.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = entryId,
            UserId = UserAId,
            CardIdentifier = "eoe001",
            TreatmentKey = "regular",
            Quantity = 1,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AcquisitionPrice = 1.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GeneralUser_CannotRead_AnotherUsersCollection()
    {
        var factory = BuildFactory($"CrossUser_ReadForbidden_{Guid.NewGuid()}");
        await SeedAsync(factory, Guid.NewGuid());

        var client = ClientAs(factory, UserBId, "GeneralUser");
        var response = await client.GetAsync($"/api/collection?userId={UserAId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanRead_AnotherUsersCollection()
    {
        var factory = BuildFactory($"CrossUser_AdminRead_{Guid.NewGuid()}");
        await SeedAsync(factory, Guid.NewGuid());

        var client = ClientAs(factory, AdminId, "Admin");
        var response = await client.GetAsync($"/api/collection?userId={UserAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("EOE001", body);
    }

    [Fact]
    public async Task Admin_CannotModify_AnotherUsersEntry()
    {
        var factory = BuildFactory($"CrossUser_AdminModify_{Guid.NewGuid()}");
        var entryId = Guid.NewGuid();
        await SeedAsync(factory, entryId);

        var client = ClientAs(factory, AdminId, "Admin");
        var payload = JsonSerializer.Serialize(new
        {
            cardIdentifier = "eoe001",
            treatment = "foil",
            quantity = 5,
            condition = "NM",
            autographed = false,
            acquisitionDate = "2026-01-01",
            acquisitionPrice = 2.00m,
            notes = ""
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"/api/collection/{entryId}", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
