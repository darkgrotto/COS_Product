using System.Net;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Import;

public class SlabsImportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SlabsImportTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> NewFactory(string label) =>
        ImportTestSupport.BuildFactory(_factory, $"SlabsImport_{label}_{Guid.NewGuid()}",
            new[] { "regular", "foil", "serialized" });

    private static void SeedCardsAndAgencies(AppDbContext db)
    {
        db.Cards.AddRange(
            new Card { Identifier = "eoe019", SetCode = "eoe", Name = "Sample 19", UpdatedAt = DateTime.UtcNow },
            new Card { Identifier = "eoe020", SetCode = "eoe", Name = "Sample 20", UpdatedAt = DateTime.UtcNow });

        db.GradingAgencies.AddRange(
            new GradingAgency
            {
                Code = "psa", FullName = "Professional Sports Authenticator",
                ValidationUrlTemplate = "https://psa.com/{cert}",
                SupportsDirectLookup = true, Source = AgencySource.Canonical, Active = true,
            },
            new GradingAgency
            {
                Code = "bgs", FullName = "Beckett Grading Services",
                ValidationUrlTemplate = "https://bgs.com/{cert}",
                SupportsDirectLookup = true, Source = AgencySource.Canonical, Active = true,
            });
    }

    [Fact]
    public async Task ImportTemplate_ReturnsCsvWithHeaders()
    {
        var factory = NewFactory("Template");
        await ImportTestSupport.SeedAsync(factory, SeedCardsAndAgencies);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.GetAsync("/api/slabs/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith(
            "CardIdentifier,Treatment,GradingAgency,Grade,CertificateNumber,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            body);
    }

    [Fact]
    public async Task Import_ValidRows_AddsEntries()
    {
        var factory = NewFactory("Valid");
        await ImportTestSupport.SeedAsync(factory, SeedCardsAndAgencies);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment,GradingAgency,Grade,CertificateNumber,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            "EOE019,regular,PSA,9.5,11111111,,,NM,false,2026-01-15,89.99,",
            "EOE020,foil,BGS,9.5,22222222,1,100,NM,true,2026-02-10,199.00,",
        });
        var response = await client.PostAsync("/api/slabs/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Failed);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.SlabEntries.Where(s => s.UserId == ImportTestSupport.TestUserId).ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, s => Assert.Contains(s.GradingAgencyCode, new[] { "psa", "bgs" }));
    }

    [Fact]
    public async Task Import_InvalidRows_ReportsEachFailureMode()
    {
        var factory = NewFactory("Invalid");
        await ImportTestSupport.SeedAsync(factory, SeedCardsAndAgencies);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment,GradingAgency,Grade,CertificateNumber,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            ",regular,PSA,9.5,11111111,,,NM,false,2026-01-15,89.99,missing id",
            "EOE0123,regular,PSA,9.5,22222222,,,NM,false,2026-01-15,89.99,bad id",
            "EOE019,sparkle,PSA,9.5,33333333,,,NM,false,2026-01-15,89.99,bad treatment",
            "EOE019,regular,UNKNOWN,9.5,44444444,,,NM,false,2026-01-15,89.99,bad agency",
            "EOE019,regular,PSA,,55555555,,,NM,false,2026-01-15,89.99,missing grade",
            "EOE019,regular,PSA,9.5,,,,NM,false,2026-01-15,89.99,missing cert",
            "EOE019,regular,PSA,9.5,66666666,5,,NM,false,2026-01-15,89.99,serial without print run",
            "EOE019,regular,PSA,9.5,77777777,500,100,NM,false,2026-01-15,89.99,serial > print run",
            "EOE019,regular,PSA,9.5,88888888,,,Pristine,false,2026-01-15,89.99,bad condition",
            "EOE019,regular,PSA,9.5,99999999,,,NM,false,not-a-date,89.99,bad date",
            "EOE019,regular,PSA,9.5,10101010,,,NM,false,2026-01-15,-12.00,negative price",
            "EOE019,regular,PSA,9.5,21212121,,,NM,false,2026-01-15,89.99,first dup",
            "EOE019,regular,PSA,9.5,21212121,,,NM,false,2026-01-15,89.99,second dup",
            "zzz9999,regular,PSA,9.5,32323232,,,NM,false,2026-01-15,89.99,card not in db",
        });
        var response = await client.PostAsync("/api/slabs/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(1, result.Added); // only the first dup row passes
        Assert.True(result.Failed >= 12, $"expected at least 12 failures, got {result.Failed}");
        Assert.Contains(result.Failures, f => f.Contains("CardIdentifier is required"));
        Assert.Contains(result.Failures, f => f.Contains("invalid card identifier 'EOE0123'"));
        Assert.Contains(result.Failures, f => f.Contains("unknown treatment 'sparkle'"));
        Assert.Contains(result.Failures, f => f.Contains("unknown GradingAgency 'UNKNOWN'"));
        Assert.Contains(result.Failures, f => f.Contains("Grade is required"));
        Assert.Contains(result.Failures, f => f.Contains("CertificateNumber is required"));
        Assert.Contains(result.Failures, f => f.Contains("SerialNumber and PrintRunTotal must be provided together"));
        Assert.Contains(result.Failures, f => f.Contains("cannot exceed PrintRunTotal"));
        Assert.Contains(result.Failures, f => f.Contains("invalid Condition"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionDate is required"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionPrice must be a non-negative number"));
        Assert.Contains(result.Failures, f => f.Contains("duplicate certificate PSA 21212121 within file"));
        Assert.Contains(result.Failures, f => f.Contains("Card not found in database: ZZZ9999"));
    }

    [Fact]
    public async Task Import_DuplicateCertAcrossUploads_BlockedAsFailure()
    {
        var factory = NewFactory("ExistingCert");
        await ImportTestSupport.SeedAsync(factory, db =>
        {
            SeedCardsAndAgencies(db);
            db.SlabEntries.Add(new SlabEntry
            {
                Id = Guid.NewGuid(),
                UserId = ImportTestSupport.TestUserId,
                CardIdentifier = "eoe019",
                TreatmentKey = "regular",
                GradingAgencyCode = "psa",
                Grade = "9.5",
                CertificateNumber = "11111111",
                Condition = CardCondition.NM,
                AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                AcquisitionPrice = 50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        });
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "CardIdentifier,Treatment,GradingAgency,Grade,CertificateNumber,SerialNumber,PrintRunTotal,Condition,Autographed,AcquisitionDate,AcquisitionPrice,Notes",
            "EOE019,regular,PSA,9.5,11111111,,,NM,false,2026-01-15,89.99,",
            "EOE019,regular,PSA,9.5,99999999,,,NM,false,2026-01-15,89.99,",
        });
        var response = await client.PostAsync("/api/slabs/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Duplicate slab: PSA 11111111"));
    }

    [Fact]
    public async Task Import_NoFile_Returns400()
    {
        var factory = NewFactory("NoFile");
        await ImportTestSupport.SeedAsync(factory, SeedCardsAndAgencies);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.PostAsync("/api/slabs/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Unauthenticated_Returns401()
    {
        var factory = NewFactory("Unauth");
        await ImportTestSupport.SeedAsync(factory, SeedCardsAndAgencies);
        var client = ImportTestSupport.ClientWithoutAuth(factory);

        var response = await client.PostAsync("/api/slabs/import",
            ImportTestSupport.CsvPayload("CardIdentifier,Treatment\nEOE019,regular\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
