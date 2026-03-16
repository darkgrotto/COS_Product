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
public class SetCompletionTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public SetCompletionTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<Guid> SeedUserAndSetAsync(AppDbContext db, string setCode, int totalCards)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        if (!await db.Sets.AnyAsync(s => s.Code == setCode))
        {
            db.Sets.Add(new Set
            {
                Code = setCode,
                Name = $"Set {setCode.ToUpperInvariant()}",
                TotalCards = totalCards,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Treatments.AnyAsync(t => t.Key == "regular"))
            db.Treatments.Add(new Treatment { Key = "regular", DisplayName = "Regular", SortOrder = 1 });
        if (!await db.Treatments.AnyAsync(t => t.Key == "foil"))
            db.Treatments.Add(new Treatment { Key = "foil", DisplayName = "Foil", SortOrder = 2 });

        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task SetCompletion_CountsBothTreatments_WhenRegularOnlyFalse()
    {
        await using var db = _fixture.CreateContext();
        var setCode = $"sc{Guid.NewGuid():N}".Substring(0, 4);
        var userId = await SeedUserAndSetAsync(db, setCode, 10);

        // Add card 001 as regular, card 002 as foil (different cards)
        db.Cards.Add(new Card { Identifier = $"{setCode}001", SetCode = setCode, Name = "Card 1", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        db.Cards.Add(new Card { Identifier = $"{setCode}002", SetCode = setCode, Name = "Card 2", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = $"{setCode}001",
            TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = $"{setCode}002",
            TreatmentKey = "foil", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetSetCompletionAsync(userId, setCode, regularOnly: false);

        Assert.Equal(2, result.OwnedCount);
    }

    [Fact]
    public async Task SetCompletion_CountsOnlyRegular_WhenRegularOnlyTrue()
    {
        await using var db = _fixture.CreateContext();
        var setCode = $"sr{Guid.NewGuid():N}".Substring(0, 4);
        var userId = await SeedUserAndSetAsync(db, setCode, 10);

        db.Cards.Add(new Card { Identifier = $"{setCode}001", SetCode = setCode, Name = "Card 1", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        db.Cards.Add(new Card { Identifier = $"{setCode}002", SetCode = setCode, Name = "Card 2", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = $"{setCode}001",
            TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = $"{setCode}002",
            TreatmentKey = "foil", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var metricsService = new MetricsService(db);
        var result = await metricsService.GetSetCompletionAsync(userId, setCode, regularOnly: true);

        // Only the regular-treatment card should be counted
        Assert.Equal(1, result.OwnedCount);
    }
}
