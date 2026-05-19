using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
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

public class CardSubtypeFilterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid UserId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    public CardSubtypeFilterTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> BuildFactory(string label) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var desc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (desc != null) services.Remove(desc);
                var ctxDesc = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (ctxDesc != null) services.Remove(ctxDesc);

                services.AddDbContext<AppDbContext>(
                    opt => opt.UseInMemoryDatabase($"SubtypeFilter_{label}_{Guid.NewGuid()}"),
                    optionsLifetime: ServiceLifetime.Singleton);

                services.AddAuthentication("HeaderDriven")
                    .AddScheme<AuthenticationSchemeOptions, HeaderDrivenAuthHandler>(
                        "HeaderDriven", _ => { });
            });
        });

    private static async Task SeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new User
        {
            Id = UserId, Username = "subtypeuser", PasswordHash = "x",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false, State = AccountState.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });

        db.Cards.AddRange(
            new Card
            {
                Identifier = "eoe001", SetCode = "eoe", Name = "Wizard Card",
                CardType = "Legendary Creature — Human Wizard",
                CardSubtypes = "Human,Wizard",
                UpdatedAt = DateTime.UtcNow,
            },
            new Card
            {
                Identifier = "eoe002", SetCode = "eoe", Name = "Goblin Card",
                CardType = "Creature — Goblin",
                CardSubtypes = "Goblin",
                UpdatedAt = DateTime.UtcNow,
            },
            new Card
            {
                Identifier = "eoe003", SetCode = "eoe", Name = "Plains",
                CardType = "Basic Land — Plains",
                CardSubtypes = "Plains",
                UpdatedAt = DateTime.UtcNow,
            });

        foreach (var id in new[] { "eoe001", "eoe002", "eoe003" })
            db.CollectionEntries.Add(new CollectionEntry
            {
                Id = Guid.NewGuid(), UserId = UserId,
                CardIdentifier = id, TreatmentKey = "regular",
                Quantity = 1, Condition = CardCondition.NM,
                AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                AcquisitionPrice = 1m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });

        await db.SaveChangesAsync();
    }

    private static HttpClient ClientAs(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", UserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-User-Role", "GeneralUser");
        return client;
    }

    [Fact]
    public async Task FilterByCardSubtype_ReturnsOnlyMatchingEntries()
    {
        var factory = BuildFactory("Wizard");
        await SeedAsync(factory);

        var client = ClientAs(factory);
        var response = await client.GetAsync("/api/collection?filter.cardSubtype=Wizard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("EOE001", body);
        Assert.DoesNotContain("EOE002", body);
        Assert.DoesNotContain("EOE003", body);
    }

    [Fact]
    public async Task FilterByCardSubtype_NoMatches_ReturnsEmpty()
    {
        var factory = BuildFactory("NoMatch");
        await SeedAsync(factory);

        var client = ClientAs(factory);
        var response = await client.GetAsync("/api/collection?filter.cardSubtype=Dragon");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("EOE001", body);
        Assert.DoesNotContain("EOE002", body);
        Assert.DoesNotContain("EOE003", body);
    }

    [Fact]
    public async Task FilterByCardSubtype_Combinable_WithCardType()
    {
        var factory = BuildFactory("Combined");
        await SeedAsync(factory);

        var client = ClientAs(factory);
        // Creature + Goblin should match only EOE002.
        var response = await client.GetAsync("/api/collection?filter.cardType=Creature&filter.cardSubtype=Goblin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("EOE002", body);
        Assert.DoesNotContain("EOE001", body);
        Assert.DoesNotContain("EOE003", body);
    }

    [Fact]
    public async Task NoSubtypeFilter_ReturnsAll()
    {
        var factory = BuildFactory("Unfiltered");
        await SeedAsync(factory);

        var client = ClientAs(factory);
        var response = await client.GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("EOE001", body);
        Assert.Contains("EOE002", body);
        Assert.Contains("EOE003", body);
    }
}
