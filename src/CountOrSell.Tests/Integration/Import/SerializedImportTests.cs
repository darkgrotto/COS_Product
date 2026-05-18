using System.Net;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Import;

public class SerializedImportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SerializedImportTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> NewFactory(string label) =>
        ImportTestSupport.BuildFactory(_factory, $"SerializedImport_{label}_{Guid.NewGuid()}",
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

        var response = await client.GetAsync("/api/serialized/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith(
            "CardIdentifier,Treatment,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            body);
    }

    [Fact]
    public async Task Import_ValidRows_AddsEntries()
    {
        var factory = NewFactory("Valid");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            "EOE019,serialized,42,500,NM,false,2026-01-15,49.99,first",
            "EOE020,foil,1,100,LP,true,2026-02-01,79.50,second",
        });
        var response = await client.PostAsync("/api/serialized/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Failed);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.SerializedEntries.Where(s => s.UserId == ImportTestSupport.TestUserId).ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, s => s.SerialNumber == 42 && s.PrintRunTotal == 500);
    }

    [Fact]
    public async Task Import_InvalidRows_ReportsEachFailureMode()
    {
        var factory = NewFactory("Invalid");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            ",serialized,1,100,NM,false,2026-01-15,49.99,missing id",
            "EOE0123,serialized,1,100,NM,false,2026-01-15,49.99,bad id",
            "EOE019,sparkle,1,100,NM,false,2026-01-15,49.99,bad treatment",
            "EOE019,serialized,0,100,NM,false,2026-01-15,49.99,zero serial",
            "EOE019,serialized,5,not-a-number,NM,false,2026-01-15,49.99,bad print run",
            "EOE019,serialized,500,100,NM,false,2026-01-15,49.99,serial > print run",
            "EOE019,serialized,1,100,Pristine,false,2026-01-15,49.99,bad condition",
            "EOE019,serialized,1,100,NM,false,not-a-date,49.99,bad date",
            "EOE019,serialized,1,100,NM,false,2026-01-15,-5.00,negative price",
            "zzz9999,serialized,1,100,NM,false,2026-01-15,49.99,card not in db",
        });
        var response = await client.PostAsync("/api/serialized/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(0, result.Added);
        Assert.Equal(10, result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("CardIdentifier is required"));
        Assert.Contains(result.Failures, f => f.Contains("invalid card identifier 'EOE0123'"));
        Assert.Contains(result.Failures, f => f.Contains("unknown treatment 'sparkle'"));
        Assert.Contains(result.Failures, f => f.Contains("SerialNumber must be a positive integer"));
        Assert.Contains(result.Failures, f => f.Contains("PrintRunTotal must be a positive integer"));
        Assert.Contains(result.Failures, f => f.Contains("cannot exceed PrintRunTotal"));
        Assert.Contains(result.Failures, f => f.Contains("invalid Condition"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionDate is required"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionPrice must be a non-negative number"));
        Assert.Contains(result.Failures, f => f.Contains("Card not found in database: ZZZ9999"));
    }

    [Fact]
    public async Task Import_NoFile_Returns400()
    {
        var factory = NewFactory("NoFile");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.PostAsync("/api/serialized/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Unauthenticated_Returns401()
    {
        var factory = NewFactory("Unauth");
        await ImportTestSupport.SeedAsync(factory, SeedCards);
        var client = ImportTestSupport.ClientWithoutAuth(factory);

        var response = await client.PostAsync("/api/serialized/import",
            ImportTestSupport.CsvPayload("CardIdentifier,Treatment\nEOE019,regular\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
