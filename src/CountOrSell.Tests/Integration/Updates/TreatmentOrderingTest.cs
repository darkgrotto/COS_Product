using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

[Trait("Category", "RequiresDocker")]
public class TreatmentOrderingTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public TreatmentOrderingTest(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplyContentUpdate_SavesTreatmentsSetsAndCards_InCorrectOrder()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var taxonomy = new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance);
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, NullLogger<ContentUpdateApplicator>.Instance);

        var setCode = $"t{Guid.NewGuid():N}".Substring(0, 4); // 4 lowercase chars
        var cardId = $"{setCode}001";
        var treatmentKey = $"ord-{Guid.NewGuid():N}".Substring(0, 12);
        var contentVersion = $"v-order-{Guid.NewGuid():N}";

        var treatments = new[]
        {
            new { key = treatmentKey, displayName = "Order Test", sortOrder = 5 }
        };
        var sets = new[]
        {
            new { code = setCode, name = "Order Test Set", totalCards = 1, releaseDate = (string?)null }
        };
        var cards = new[]
        {
            new
            {
                identifier = cardId,
                setCode,
                name = "Order Test Card",
                color = "Blue",
                cardType = "Creature",
                marketValue = (decimal?)1.50m
            }
        };

        using var packageStream = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards);

        await applicator.ApplyContentUpdateAsync(packageStream, contentVersion, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();

        var treatmentSaved = await verifyDb.Treatments.AnyAsync(t => t.Key == treatmentKey);
        Assert.True(treatmentSaved, "Treatment should be saved");

        var setSaved = await verifyDb.Sets.AnyAsync(s => s.Code == setCode);
        Assert.True(setSaved, "Set should be saved");

        var cardSaved = await verifyDb.Cards.AnyAsync(c => c.Identifier == cardId);
        Assert.True(cardSaved, "Card should be saved");

        var updateVersion = await verifyDb.UpdateVersions
            .FirstOrDefaultAsync(u => u.ContentVersion == contentVersion);
        Assert.NotNull(updateVersion);
        Assert.Equal(contentVersion, updateVersion.ContentVersion);
    }

    [Fact]
    public async Task ApplyContentUpdate_UpsertsTreatment_WhenAlreadyExists()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var taxonomy = new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance);
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, NullLogger<ContentUpdateApplicator>.Instance);

        var treatmentKey = $"upsert-{Guid.NewGuid():N}".Substring(0, 14);
        var setCode = $"u{Guid.NewGuid():N}".Substring(0, 4);
        var contentVersion1 = $"v-upsert1-{Guid.NewGuid():N}";
        var contentVersion2 = $"v-upsert2-{Guid.NewGuid():N}";

        var sets = new[]
        {
            new { code = setCode, name = "Upsert Test Set", totalCards = 0, releaseDate = (string?)null }
        };

        // First apply - create the treatment
        var treatments1 = new[]
        {
            new { key = treatmentKey, displayName = "Original Name", sortOrder = 1 }
        };
        using var package1 = PackageBuilder.Build(treatments: treatments1, sets: sets);
        await applicator.ApplyContentUpdateAsync(package1, contentVersion1, CancellationToken.None);

        // Second apply - update the treatment display name
        await using var db2 = _fixture.CreateContext();
        var taxonomy2 = new SealedTaxonomyRepository(db2, NullLogger<SealedTaxonomyRepository>.Instance);
        var applicator2 = new ContentUpdateApplicator(
            db2, imageStore, taxonomy2, NullLogger<ContentUpdateApplicator>.Instance);
        var treatments2 = new[]
        {
            new { key = treatmentKey, displayName = "Updated Name", sortOrder = 2 }
        };
        using var package2 = PackageBuilder.Build(treatments: treatments2, sets: sets);
        await applicator2.ApplyContentUpdateAsync(package2, contentVersion2, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();
        var treatment = await verifyDb.Treatments.FindAsync(treatmentKey);
        Assert.NotNull(treatment);
        Assert.Equal("Updated Name", treatment.DisplayName);
        Assert.Equal(2, treatment.SortOrder);
    }
}
