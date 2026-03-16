using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Collection;

[Trait("Category", "RequiresDocker")]
public class UniversalFilterTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public UniversalFilterTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<Guid> SeedUserAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private static async Task SeedCardDataAsync(AppDbContext db)
    {
        foreach (var setCode in new[] { "uf1", "uf2" })
        {
            if (!await db.Sets.AnyAsync(s => s.Code == setCode))
                db.Sets.Add(new Set { Code = setCode, Name = $"Filter Set {setCode}", TotalCards = 5, UpdatedAt = DateTime.UtcNow });
        }

        var cardData = new[]
        {
            ("uf1001", "uf1"), ("uf1002", "uf1"), ("uf2001", "uf2")
        };
        foreach (var (id, set) in cardData)
        {
            if (!await db.Cards.AnyAsync(c => c.Identifier == id))
                db.Cards.Add(new Card { Identifier = id, SetCode = set, Name = $"Card {id}", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        }

        foreach (var key in new[] { "regular", "foil" })
        {
            if (!await db.Treatments.AnyAsync(t => t.Key == key))
                db.Treatments.Add(new Treatment { Key = key, DisplayName = key, SortOrder = 1 });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Filter_BySetCode_ReturnsOnlyMatchingEntries()
    {
        await using var db = _fixture.CreateContext();
        await SeedCardDataAsync(db);
        var userId = await SeedUserAsync(db);

        db.CollectionEntries.AddRange(
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf1001", TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf2001", TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = new CollectionRepository(db);
        var filter = new CollectionFilter { SetCode = "uf1" };
        var results = await repo.GetByUserFilteredAsync(userId, filter);

        Assert.All(results, e => Assert.StartsWith("uf1", e.CardIdentifier));
        Assert.Single(results);
    }

    [Fact]
    public async Task Filter_ByCondition_ReturnsOnlyMatchingEntries()
    {
        await using var db = _fixture.CreateContext();
        await SeedCardDataAsync(db);
        var userId = await SeedUserAsync(db);

        db.CollectionEntries.AddRange(
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf1001", TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf1002", TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.LP, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = new CollectionRepository(db);
        var filter = new CollectionFilter { Condition = "NM" };
        var results = await repo.GetByUserFilteredAsync(userId, filter);

        Assert.All(results, e => Assert.Equal(CardCondition.NM, e.Condition));
    }

    [Fact]
    public async Task Filter_ByTreatment_ReturnsOnlyMatchingEntries()
    {
        await using var db = _fixture.CreateContext();
        await SeedCardDataAsync(db);
        var userId = await SeedUserAsync(db);

        db.CollectionEntries.AddRange(
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf1001", TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CollectionEntry { Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "uf1002", TreatmentKey = "foil", Quantity = 1, Condition = CardCondition.NM, Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today), AcquisitionPrice = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = new CollectionRepository(db);
        var filter = new CollectionFilter { Treatment = "foil" };
        var results = await repo.GetByUserFilteredAsync(userId, filter);

        Assert.All(results, e => Assert.Equal("foil", e.TreatmentKey));
        Assert.Single(results);
    }
}
