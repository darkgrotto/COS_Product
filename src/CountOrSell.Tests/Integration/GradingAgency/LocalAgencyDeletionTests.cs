using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.GradingAgencies;

[Trait("Category", "RequiresDocker")]
public class LocalAgencyDeletionTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public LocalAgencyDeletionTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> SeedLocalAgencyAsync(AppDbContext db)
    {
        var code = $"la{Guid.NewGuid():N}".Substring(0, 6);
        db.GradingAgencies.Add(new CountOrSell.Domain.Models.GradingAgency
        {
            Code = code,
            FullName = "Local Test Agency",
            ValidationUrlTemplate = "https://example.com/verify/{0}",
            SupportsDirectLookup = true,
            Source = AgencySource.Local,
            Active = true
        });
        await db.SaveChangesAsync();
        return code;
    }

    [Fact]
    public async Task DeleteLocalAgency_WithNoSlabRecords_DeletesImmediately()
    {
        await using var db = _fixture.CreateContext();
        var code = await SeedLocalAgencyAsync(db);

        var repo = new GradingAgencyRepository(db);
        var slabRepo = new SlabRepository(db);

        var count = await slabRepo.CountByAgencyCodeAsync(code);
        Assert.Equal(0, count);

        await repo.DeleteAsync(code);

        await using var verifyDb = _fixture.CreateContext();
        var deleted = await verifyDb.GradingAgencies.FindAsync(code);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteLocalAgency_WithSlabRecords_RemapsAndDeletes()
    {
        await using var db = _fixture.CreateContext();
        var localCode = await SeedLocalAgencyAsync(db);

        // Create a user and a slab entry referencing the local agency
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            State = AccountState.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        // Seed treatment and card
        if (!await db.Treatments.AnyAsync(t => t.Key == "regular"))
            db.Treatments.Add(new Treatment { Key = "regular", DisplayName = "Regular", SortOrder = 1 });
        if (!await db.Sets.AnyAsync(s => s.Code == "tst"))
            db.Sets.Add(new Set { Code = "tst", Name = "Test Set", TotalCards = 5, UpdatedAt = DateTime.UtcNow });
        if (!await db.Cards.AnyAsync(c => c.Identifier == "tst001"))
            db.Cards.Add(new Card { Identifier = "tst001", SetCode = "tst", Name = "Card 1", CurrentMarketValue = 1m, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.SlabEntries.Add(new SlabEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "tst001",
            TreatmentKey = "regular", GradingAgencyCode = localCode,
            Grade = "9", CertificateNumber = "12345",
            Condition = CardCondition.NM, Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 50m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var slabRepo = new SlabRepository(db);
        var agencyRepo = new GradingAgencyRepository(db);

        var count = await slabRepo.CountByAgencyCodeAsync(localCode);
        Assert.Equal(1, count);

        // Remap to PSA (canonical) and delete local agency
        await slabRepo.RemapAgencyCodeAsync(localCode, "psa");
        await agencyRepo.DeleteAsync(localCode);

        await using var verifyDb = _fixture.CreateContext();

        var deletedAgency = await verifyDb.GradingAgencies.FindAsync(localCode);
        Assert.Null(deletedAgency);

        var remappedSlab = await verifyDb.SlabEntries
            .FirstOrDefaultAsync(s => s.UserId == userId);
        Assert.NotNull(remappedSlab);
        Assert.Equal("psa", remappedSlab.GradingAgencyCode);
    }
}
