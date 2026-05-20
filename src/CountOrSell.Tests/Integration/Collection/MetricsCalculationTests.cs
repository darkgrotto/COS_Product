using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Collection;

[Trait("Category", "RequiresDocker")]
public class MetricsCalculationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public MetricsCalculationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task SeedCardAndSet(AppDbContext db, string setCode, string cardIdentifier, decimal marketValue)
    {
        if (!await db.Sets.AnyAsync(s => s.Code == setCode))
        {
            db.Sets.Add(new Set
            {
                Code = setCode,
                Name = $"Set {setCode.ToUpperInvariant()}",
                TotalCards = 10,
                UpdatedAt = DateTime.UtcNow
            });
        }
        if (!await db.Cards.AnyAsync(c => c.Identifier == cardIdentifier))
        {
            db.Cards.Add(new Card
            {
                Identifier = cardIdentifier,
                SetCode = setCode,
                Name = $"Card {cardIdentifier.ToUpperInvariant()}",
                CurrentMarketValue = marketValue,
                UpdatedAt = DateTime.UtcNow
            });
        }
        if (!await db.Treatments.AnyAsync(t => t.Key == "regular"))
        {
            db.Treatments.Add(new Treatment { Key = "regular", DisplayName = "Regular", SortOrder = 1 });
        }
        if (!await db.Treatments.AnyAsync(t => t.Key == "foil"))
        {
            db.Treatments.Add(new Treatment { Key = "foil", DisplayName = "Foil", SortOrder = 2 });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMetrics_TotalValue_EqualsMarketValueTimesQuantity()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedCardAndSet(db, "tst", "tst001", 5.00m);
        await SeedCardAndSet(db, "tst", "tst002", 10.00m);

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst001",
            TreatmentKey = "regular", Quantity = 3, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 4.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst002",
            TreatmentKey = "foil", Quantity = 2, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 8.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        // Total value = 5.00 * 3 + 10.00 * 2 = 15.00 + 20.00 = 35.00
        Assert.Equal(35.00m, result.TotalValue);
        // P/L = (5.00 - 4.00) * 3 + (10.00 - 8.00) * 2 = 3.00 + 4.00 = 7.00
        Assert.Equal(7.00m, result.TotalProfitLoss);
        Assert.Equal(5, result.TotalCardCount);
    }

    [Fact]
    public async Task GetMetrics_ByContentType_BreaksDownCorrectly()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedCardAndSet(db, "tst", "tst003", 20.00m);

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst003",
            TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 15.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        var cardBreakdown = result.ByContentType.FirstOrDefault(b => b.ContentType == "cards");
        Assert.NotNull(cardBreakdown);
        Assert.Equal(20.00m, cardBreakdown.TotalValue);
        Assert.Equal(5.00m, cardBreakdown.TotalProfitLoss);
    }

    [Fact]
    public async Task GetMetrics_FoilEntry_UsesPerTreatmentPriceFromCardPrices()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Card.CurrentMarketValue holds the "regular" treatment price (1.00). The foil
        // row in card_prices is what the foil collection entry should be priced at.
        await SeedCardAndSet(db, "tst", "tst010", 1.00m);
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst010",
            TreatmentKey = "regular",
            PriceUsd = 1.00m,
            CapturedAt = DateTime.UtcNow
        });
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst010",
            TreatmentKey = "foil",
            PriceUsd = 25.00m,
            CapturedAt = DateTime.UtcNow
        });

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst010",
            TreatmentKey = "foil", Quantity = 2, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 10.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        // Expected: foil price (25.00) * quantity (2) = 50.00
        // Bug behavior would be: regular price (1.00) * quantity (2) = 2.00
        Assert.Equal(50.00m, result.TotalValue);
        // P/L: (25.00 - 10.00) * 2 = 30.00
        Assert.Equal(30.00m, result.TotalProfitLoss);
    }

    [Fact]
    public async Task GetMetrics_TreatmentNotInCardPrices_FallsBackToCardCurrentMarketValue()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Card.CurrentMarketValue = 3.00; no card_prices rows at all - both regular
        // and foil entries should fall back to Card.CurrentMarketValue.
        await SeedCardAndSet(db, "tst", "tst011", 3.00m);

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst011",
            TreatmentKey = "foil", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 2.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        Assert.Equal(3.00m, result.TotalValue);
        Assert.Equal(1.00m, result.TotalProfitLoss);
    }

    [Fact]
    public async Task GetMetrics_Serialized_UsesPerTreatmentPrice()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedCardAndSet(db, "tst", "tst012", 5.00m);
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst012",
            TreatmentKey = "foil",
            PriceUsd = 100.00m,
            CapturedAt = DateTime.UtcNow
        });

        db.SerializedEntries.Add(new SerializedEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst012",
            TreatmentKey = "foil", SerialNumber = 1, PrintRunTotal = 100,
            Condition = CardCondition.NM, Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 50.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        var serializedBreakdown = result.ByContentType.First(b => b.ContentType == "serialized");
        Assert.Equal(100.00m, serializedBreakdown.TotalValue);
        Assert.Equal(50.00m, serializedBreakdown.TotalProfitLoss);
    }

    [Fact]
    public async Task GetMetrics_Slabs_UsesPerTreatmentPrice()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedCardAndSet(db, "tst", "tst013", 7.00m);
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst013",
            TreatmentKey = "foil",
            PriceUsd = 200.00m,
            CapturedAt = DateTime.UtcNow
        });

        // Grading agency required for slab entry
        if (!await db.GradingAgencies.AnyAsync(g => g.Code == "psa"))
        {
            db.GradingAgencies.Add(new GradingAgency
            {
                Code = "psa", FullName = "PSA", ValidationUrlTemplate = "https://x/{cert}",
                SupportsDirectLookup = true, Source = AgencySource.Canonical, Active = true
            });
            await db.SaveChangesAsync();
        }

        db.SlabEntries.Add(new SlabEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst013",
            TreatmentKey = "foil", GradingAgencyCode = "psa", Grade = "10",
            CertificateNumber = "12345", Condition = CardCondition.NM, Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 80.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetMetricsAsync(userId, new CountOrSell.Domain.Models.CollectionFilter());

        var slabBreakdown = result.ByContentType.First(b => b.ContentType == "slabs");
        Assert.Equal(200.00m, slabBreakdown.TotalValue);
        Assert.Equal(120.00m, slabBreakdown.TotalProfitLoss);
    }

    [Fact]
    public async Task GetTopCards_TwoTreatmentsOfSameCard_ReturnTwoRowsDistinguishedByTreatmentKey()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await SeedCardAndSet(db, "tst", "tst020", 1.00m);
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst020", TreatmentKey = "regular",
            PriceUsd = 1.00m, CapturedAt = DateTime.UtcNow
        });
        db.CardPrices.Add(new CardPrice
        {
            CardIdentifier = "tst020", TreatmentKey = "foil",
            PriceUsd = 25.00m, CapturedAt = DateTime.UtcNow
        });

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst020",
            TreatmentKey = "regular", Quantity = 2, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 0.50m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst020",
            TreatmentKey = "foil", Quantity = 3, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 10.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var (results, totalCount) = await metricsService.GetTopCardsAsync(
            userId, metric: "value", limit: 100, offset: 0,
            filter: new CountOrSell.Domain.Models.CollectionFilter());

        Assert.Equal(2, totalCount);
        Assert.Equal(2, results.Count);

        var regularRow = results.Single(r => r.TreatmentKey == "regular");
        Assert.Equal("TST020", regularRow.CardIdentifier);
        Assert.Equal(1.00m, regularRow.MarketValue);
        Assert.Equal(2, regularRow.TotalQuantity);
        Assert.Equal(2.00m, regularRow.TotalValue);

        var foilRow = results.Single(r => r.TreatmentKey == "foil");
        Assert.Equal("TST020", foilRow.CardIdentifier);
        Assert.Equal(25.00m, foilRow.MarketValue);
        Assert.Equal(3, foilRow.TotalQuantity);
        Assert.Equal(75.00m, foilRow.TotalValue);
    }
}
