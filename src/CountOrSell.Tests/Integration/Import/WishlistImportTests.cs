using System.Net;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Import;

public class WishlistImportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WishlistImportTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> NewFactory(string label) =>
        ImportTestSupport.BuildFactory(_factory, $"WishlistImport_{label}_{Guid.NewGuid()}",
            new[] { "regular", "foil", "serialized" });

    private static void SeedCards(AppDbContext db) =>
        db.Cards.AddRange(
            new Card { Identifier = "eoe019", SetCode = "eoe", Name = "Sample 19", UpdatedAt = DateTime.UtcNow },
            new Card { Identifier = "eoe020", SetCode = "eoe", Name = "Sample 20", UpdatedAt = DateTime.UtcNow });

    [Fact]
    public async Task ImportTemplate_ReturnsCsvWithHeaders()
    {
        var factory = NewFactory("Template");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.GetAsync("/api/wishlist/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("CardIdentifier,Treatment", body);
    }

    [Fact]
    public async Task Import_ValidRows_AddsEntries()
    {
        var factory = NewFactory("Valid");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = "CardIdentifier,Treatment\nEOE019,regular\nEOE020,foil\n";
        var response = await client.PostAsync("/api/wishlist/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Failed);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.WishlistEntries.Where(w => w.UserId == ImportTestSupport.TestUserId).ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task Import_MixedValidAndInvalid_ReportsFailuresPerRow()
    {
        var factory = NewFactory("Mixed");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment",
            ",regular",                  // missing identifier
            "EOE0123,regular",           // zero-padded 4-digit -> invalid identifier
            "EOE019,sparkle",            // unknown treatment
            "EOE019,regular",            // valid -> first
            "EOE019,regular",            // duplicate within file
            "zzz9999,regular",           // valid format, not in DB -> Card not found
            "EOE020,foil",               // valid
        });
        var response = await client.PostAsync("/api/wishlist/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(2, result.Added);
        Assert.True(result.Failed >= 5, $"expected at least 5 failures, got {result.Failed}");
        Assert.Contains(result.Failures, f => f.Contains("CardIdentifier is required"));
        Assert.Contains(result.Failures, f => f.Contains("invalid card identifier 'EOE0123'"));
        Assert.Contains(result.Failures, f => f.Contains("unknown treatment 'sparkle'"));
        Assert.Contains(result.Failures, f => f.Contains("duplicate row"));
        Assert.Contains(result.Failures, f => f.Contains("Card not found in database: ZZZ9999"));
    }

    [Fact]
    public async Task Import_AlreadyOnWishlist_SkippedNotFailed()
    {
        var factory = NewFactory("Skipped");
        await ImportTestSupport.SeedAsync(factory, db =>
        {
            SeedCards(db);
            db.WishlistEntries.Add(new WishlistEntry
            {
                Id = Guid.NewGuid(),
                UserId = ImportTestSupport.TestUserId,
                CardIdentifier = "eoe019",
                TreatmentKey = "regular",
                CreatedAt = DateTime.UtcNow,
            });
        });
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = "CardIdentifier,Treatment\nEOE019,regular\nEOE020,regular\n";
        var response = await client.PostAsync("/api/wishlist/import", ImportTestSupport.CsvPayload(csv));

        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Import_NoFile_Returns400()
    {
        var factory = NewFactory("NoFile");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.PostAsync("/api/wishlist/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Unauthenticated_Returns401()
    {
        var factory = NewFactory("Unauth");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = ImportTestSupport.ClientWithoutAuth(factory);

        var response = await client.PostAsync("/api/wishlist/import",
            ImportTestSupport.CsvPayload("CardIdentifier,Treatment\nEOE019,regular\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
