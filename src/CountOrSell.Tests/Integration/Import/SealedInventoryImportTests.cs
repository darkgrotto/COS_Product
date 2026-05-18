using System.Net;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CountOrSell.Tests.Integration.Import;

public class SealedInventoryImportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SealedInventoryImportTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> NewFactory(string label) =>
        ImportTestSupport.BuildFactory(_factory, $"SealedInventoryImport_{label}_{Guid.NewGuid()}",
            Array.Empty<string>());

    private static void SeedProductsAndTaxonomy(AppDbContext db)
    {
        db.SealedProductCategories.Add(new SealedProductCategory
        {
            Slug = "booster",
            DisplayName = "Booster",
            SortOrder = 1,
        });
        db.SealedProductSubTypes.AddRange(
            new SealedProductSubType { Slug = "collector", CategorySlug = "booster", DisplayName = "Collector Booster", SortOrder = 1 },
            new SealedProductSubType { Slug = "play",      CategorySlug = "booster", DisplayName = "Play Booster",      SortOrder = 2 });

        db.SealedProducts.AddRange(
            new CountOrSell.Domain.Models.SealedProduct
            {
                Identifier = "eoe-collector-booster-box",
                SetCode = "eoe",
                Name = "EOE Collector Booster Box",
                CategorySlug = "booster",
                SubTypeSlug = "collector",
                CurrentMarketValue = 249.99m,
                UpdatedAt = DateTime.UtcNow,
            },
            new CountOrSell.Domain.Models.SealedProduct
            {
                Identifier = "eoe-play-booster-box",
                SetCode = "eoe",
                Name = "EOE Play Booster Box",
                CategorySlug = "booster",
                SubTypeSlug = "play",
                CurrentMarketValue = 179.99m,
                UpdatedAt = DateTime.UtcNow,
            });
    }

    [Fact]
    public async Task ImportTemplate_ReturnsCsvWithHeaders()
    {
        var factory = NewFactory("Template");
        await ImportTestSupport.SeedAsync(factory, SeedProductsAndTaxonomy);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.GetAsync("/api/sealed-inventory/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith(
            "ProductIdentifier,Quantity,Condition,AcquisitionDate,AcquisitionPrice,CategorySlug,SubTypeSlug,Notes",
            body);
    }

    [Fact]
    public async Task Import_ValidRows_AddsEntries()
    {
        var factory = NewFactory("Valid");
        await ImportTestSupport.SeedAsync(factory, SeedProductsAndTaxonomy);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "ProductIdentifier,Quantity,Condition,AcquisitionDate,AcquisitionPrice,CategorySlug,SubTypeSlug,Notes",
            "eoe-collector-booster-box,1,NM,2026-01-15,249.99,booster,collector,case",
            "eoe-play-booster-box,2,LP,2026-02-01,179.50,booster,play,",
        });
        var response = await client.PostAsync("/api/sealed-inventory/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Failed);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.SealedInventoryEntries.Where(s => s.UserId == ImportTestSupport.TestUserId).ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task Import_InvalidRows_ReportsEachFailureMode()
    {
        var factory = NewFactory("Invalid");
        await ImportTestSupport.SeedAsync(factory, SeedProductsAndTaxonomy);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var csv = string.Join('\n', new[]
        {
            "ProductIdentifier,Quantity,Condition,AcquisitionDate,AcquisitionPrice,CategorySlug,SubTypeSlug,Notes",
            ",1,NM,2026-01-15,249.99,booster,collector,missing id",
            "eoe-collector-booster-box,0,NM,2026-01-15,249.99,booster,collector,bad qty",
            "eoe-collector-booster-box,1,Pristine,2026-01-15,249.99,booster,collector,bad condition",
            "eoe-collector-booster-box,1,NM,not-a-date,249.99,booster,collector,bad date",
            "eoe-collector-booster-box,1,NM,2026-01-15,-10.00,booster,collector,bad price",
            "eoe-collector-booster-box,1,NM,2026-01-15,249.99,no-such-category,collector,bad category",
            "eoe-collector-booster-box,1,NM,2026-01-15,249.99,booster,no-such-subtype,bad subtype",
            "eoe-collector-booster-box,1,NM,2026-01-15,249.99,,collector,subtype without category",
            "no-such-product,1,NM,2026-01-15,249.99,booster,collector,product not in db",
        });
        var response = await client.PostAsync("/api/sealed-inventory/import", ImportTestSupport.CsvPayload(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ImportTestSupport.ParseResponseAsync(response);
        Assert.Equal(0, result.Added);
        Assert.Equal(9, result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("ProductIdentifier is required"));
        Assert.Contains(result.Failures, f => f.Contains("Quantity must be a positive integer"));
        Assert.Contains(result.Failures, f => f.Contains("invalid Condition"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionDate is required"));
        Assert.Contains(result.Failures, f => f.Contains("AcquisitionPrice must be a non-negative number"));
        Assert.Contains(result.Failures, f => f.Contains("unknown CategorySlug 'no-such-category'"));
        Assert.Contains(result.Failures, f => f.Contains("SubTypeSlug 'no-such-subtype' is not valid for category 'booster'"));
        Assert.Contains(result.Failures, f => f.Contains("SubTypeSlug requires CategorySlug"));
        Assert.Contains(result.Failures, f => f.Contains("Sealed product not found in database: no-such-product"));
    }

    [Fact]
    public async Task Import_NoFile_Returns400()
    {
        var factory = NewFactory("NoFile");
        await ImportTestSupport.SeedAsync(factory, SeedProductsAndTaxonomy);
        var client = await ImportTestSupport.ClientWithCsrfAsync(factory);

        var response = await client.PostAsync("/api/sealed-inventory/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Unauthenticated_Returns401()
    {
        var factory = NewFactory("Unauth");
        await ImportTestSupport.SeedAsync(factory, SeedProductsAndTaxonomy);
        var client = ImportTestSupport.ClientWithoutAuth(factory);

        var response = await client.PostAsync("/api/sealed-inventory/import",
            ImportTestSupport.CsvPayload("ProductIdentifier,Quantity,Condition,AcquisitionDate,AcquisitionPrice\nx,1,NM,2026-01-15,1.00\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
