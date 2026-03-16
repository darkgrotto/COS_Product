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
}
