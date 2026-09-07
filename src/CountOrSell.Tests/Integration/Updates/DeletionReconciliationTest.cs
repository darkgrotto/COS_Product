using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

// The package format carries no tombstone: nothing announces that an id was removed, so
// absence from a FULL package is the only deletion signal there is. A delta carries only
// what changed and must never be read as "everything else is gone".
[Trait("Category", "RequiresDocker")]
public class DeletionReconciliationTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public DeletionReconciliationTest(PostgreSqlFixture fixture) => _fixture = fixture;

    private ContentUpdateApplicator Build(CountOrSell.Data.AppDbContext db)
        => new(db, new NoOpImageStore(),
            new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance),
            new PackageVerifier(), new NoOpHttpClientFactory(), new StubTreatmentValidator(),
            NullLogger<ContentUpdateApplicator>.Instance);

    private static List<TreatmentDto> Treatments() =>
        [new() { Key = "regular", DisplayName = "Regular", SortOrder = 0 },
         new() { Key = "foil", DisplayName = "Foil", SortOrder = 1 }];

    private static (System.IO.MemoryStream, PackageManifest) Package(
        string setCode, IEnumerable<string> cardIds, string packageType)
    {
        var (stream, manifest) = PackageBuilder.Build(
            treatments: Treatments(),
            sets: [new() { Code = setCode, Name = "Reconciliation Set" }],
            cards: cardIds.Select(id => new CardDto
            {
                Identifier = id, SetCode = setCode, Name = "Card " + id, Treatments = ["regular"]
            }).ToList());
        manifest.PackageType = packageType;
        return (stream, manifest);
    }

    [Fact]
    public async Task A_Full_Package_Removes_Cards_It_No_Longer_Lists()
    {
        var setCode = "d" + Guid.NewGuid().ToString("N")[..3];
        var keep = setCode + "001";
        var pruned = setCode + "002";

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [keep, pruned], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            // The Backend pruned an orphaned card; the next full simply does not list it.
            var (stream, manifest) = Package(setCode, [keep], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using var verify = _fixture.CreateContext();
        Assert.True(await verify.Cards.AnyAsync(c => c.Identifier == keep));
        Assert.False(await verify.Cards.AnyAsync(c => c.Identifier == pruned));
    }

    [Fact]
    public async Task A_Delta_Never_Removes_Anything()
    {
        var setCode = "e" + Guid.NewGuid().ToString("N")[..3];
        var a = setCode + "001";
        var b = setCode + "002";

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [a, b], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            // A delta carries only what changed. Absence from it means "unchanged", never
            // "deleted" - reading it as authoritative would wipe most of the catalog.
            var (stream, manifest) = Package(setCode, [a], "delta");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using var verify = _fixture.CreateContext();
        Assert.True(await verify.Cards.AnyAsync(c => c.Identifier == a));
        Assert.True(await verify.Cards.AnyAsync(c => c.Identifier == b));
    }

    [Fact]
    public async Task A_Card_A_User_Owns_Is_Retired_Rather_Than_Deleted()
    {
        var setCode = "f" + Guid.NewGuid().ToString("N")[..3];
        var owned = setCode + "001";
        var other = setCode + "002";
        var userId = await SeedUserAsync();

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [owned, other], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            db.CollectionEntries.Add(new CollectionEntry
            {
                Id = Guid.NewGuid(), UserId = userId, CardIdentifier = owned,
                TreatmentKey = "regular", Quantity = 2, Condition = CardCondition.NM,
                AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow), AcquisitionPrice = 5m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            // Both cards vanish from canonical data, but one is in a user's collection.
            var (stream, manifest) = Package(setCode, [setCode + "003"], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using var verify = _fixture.CreateContext();

        // The unowned card is genuinely gone.
        Assert.False(await verify.Cards.AnyAsync(c => c.Identifier == other));

        // The owned one is kept and marked, so the entry still resolves a name and set
        // instead of degrading to a bare identifier.
        var retained = await verify.Cards.FirstOrDefaultAsync(c => c.Identifier == owned);
        Assert.NotNull(retained);
        Assert.NotNull(retained!.RetiredAt);

        // The user's holding is untouched. It is their data, not canonical data.
        var entry = await verify.CollectionEntries.FirstOrDefaultAsync(e => e.CardIdentifier == owned);
        Assert.NotNull(entry);
        Assert.Equal(2, entry!.Quantity);
    }

    [Fact]
    public async Task A_Retired_Card_Returning_In_A_Later_Full_Is_Canonical_Again()
    {
        var setCode = "g" + Guid.NewGuid().ToString("N")[..3];
        var card = setCode + "001";
        var userId = await SeedUserAsync();

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [card], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
            db.CollectionEntries.Add(new CollectionEntry
            {
                Id = Guid.NewGuid(), UserId = userId, CardIdentifier = card,
                TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM,
                AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow), AcquisitionPrice = 1m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [setCode + "009"], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            Assert.NotNull((await db.Cards.FirstAsync(c => c.Identifier == card)).RetiredAt);

            // A prune can be reversed upstream, so a returning card must become normal again.
            var (stream, manifest) = Package(setCode, [card], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using var verify = _fixture.CreateContext();
        Assert.Null((await verify.Cards.FirstAsync(c => c.Identifier == card)).RetiredAt);
    }

    [Fact]
    public async Task A_Full_Carrying_No_Card_Data_Does_Not_Read_As_Everything_Deleted()
    {
        var setCode = "h" + Guid.NewGuid().ToString("N")[..3];
        var card = setCode + "001";

        await using (var db = _fixture.CreateContext())
        {
            var (stream, manifest) = Package(setCode, [card], "full");
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using (var db = _fixture.CreateContext())
        {
            // A treatments-only full must not be mistaken for "the catalog is now empty".
            var (stream, manifest) = PackageBuilder.Build(treatments: Treatments());
            manifest.PackageType = "full";
            await Build(db).ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);
        }

        await using var verify = _fixture.CreateContext();
        Assert.True(await verify.Cards.AnyAsync(c => c.Identifier == card));
    }

    private async Task<Guid> SeedUserAsync()
    {
        await using var db = _fixture.CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "recon-" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "Reconciliation Test User",
            Role = UserRole.GeneralUser,
            State = AccountState.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}
