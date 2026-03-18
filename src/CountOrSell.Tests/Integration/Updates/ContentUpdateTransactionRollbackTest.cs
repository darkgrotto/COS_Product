using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Images;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

[Trait("Category", "RequiresDocker")]
public class ContentUpdateTransactionRollbackTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public ContentUpdateTransactionRollbackTest(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplyContentUpdate_RollsBack_WhenCardReferencesNonExistentSet()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, NullLogger<ContentUpdateApplicator>.Instance);

        // Build a package with treatments but cards referencing a set that does not exist
        var treatments = new[]
        {
            new { key = "regular", displayName = "Regular", sortOrder = 0 }
        };
        // No sets in this package - cards will reference a non-existent set_code
        var cards = new[]
        {
            new
            {
                identifier = "tst001",
                setCode = "tst",  // "tst" set does not exist in the DB
                name = "Test Card",
                color = (string?)null,
                cardType = (string?)null,
                marketValue = (decimal?)null
            }
        };

        using var packageStream = PackageBuilder.Build(treatments: treatments, cards: cards);

        // Should throw due to FK violation on cards.set_code -> sets.code
        await Assert.ThrowsAnyAsync<Exception>(() =>
            applicator.ApplyContentUpdateAsync(packageStream, "v-rollback-test", CancellationToken.None));

        // Transaction should have rolled back - treatments from the same package must NOT be persisted
        await using var verifyDb = _fixture.CreateContext();
        var treatmentExists = await verifyDb.Treatments.AnyAsync(t => t.Key == "regular");
        // Note: "regular" may already exist from other tests; we use a unique key to avoid collision
        // The test above confirmed an exception was thrown, which means the transaction was rolled back
        // We cannot easily verify "regular" wasn't inserted because other tests may have inserted it
        // The key assertion is that the update version was NOT recorded
        var updateVersionExists = await verifyDb.UpdateVersions
            .AnyAsync(u => u.ContentVersion == "v-rollback-test");
        Assert.False(updateVersionExists);
    }

    [Fact]
    public async Task ApplyContentUpdate_RollsBack_WhenCardReferencesNonExistentSet_UniqueTreatmentKey()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, NullLogger<ContentUpdateApplicator>.Instance);

        // Use a unique treatment key so we can verify it was not persisted after rollback
        var uniqueTreatmentKey = $"rollback-test-{Guid.NewGuid():N}";
        var treatments = new[]
        {
            new { key = uniqueTreatmentKey, displayName = "Rollback Test Treatment", sortOrder = 99 }
        };
        var cards = new[]
        {
            new
            {
                identifier = "zz9001",
                setCode = "zz9",  // set "zz9" does not exist
                name = "Rollback Test Card",
                color = (string?)null,
                cardType = (string?)null,
                marketValue = (decimal?)null
            }
        };

        using var packageStream = PackageBuilder.Build(treatments: treatments, cards: cards);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            applicator.ApplyContentUpdateAsync(packageStream, "v-rollback-unique", CancellationToken.None));

        // Verify the treatment was NOT persisted (transaction rolled back)
        await using var verifyDb = _fixture.CreateContext();
        var treatmentExists = await verifyDb.Treatments.AnyAsync(t => t.Key == uniqueTreatmentKey);
        Assert.False(treatmentExists);
    }
}

// Test double - no-op image store for tests that do not care about image saving.
internal sealed class NoOpImageStore : IImageStore
{
    public Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct)
        => Task.CompletedTask;

    public Task<byte[]?> GetImageAsync(string relativePath, CancellationToken ct)
        => Task.FromResult<byte[]?>(null);

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct)
        => Task.FromResult(false);
}
