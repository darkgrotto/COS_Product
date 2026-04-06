using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Packages;
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
        var verifier = new PackageVerifier();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, verifier, NullLogger<ContentUpdateApplicator>.Instance);

        var setCode = $"t{Guid.NewGuid():N}".Substring(0, 4);
        var cardId = $"{setCode}001";
        var treatmentKey = $"ord-{Guid.NewGuid():N}".Substring(0, 12);

        var treatments = new List<TreatmentDto>
        {
            new() { Key = treatmentKey, DisplayName = "Order Test", SortOrder = 5 }
        };
        var sets = new List<SetDto>
        {
            new() { Code = setCode, Name = "Order Test Set", TotalCards = 1 }
        };
        var cards = new List<CardDto>
        {
            new() { Identifier = cardId, SetCode = setCode, Name = "Order Test Card" }
        };

        var (packageStream, packageManifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards);

        await applicator.ApplyContentUpdateAsync(packageStream, packageManifest, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();

        var treatmentSaved = await verifyDb.Treatments.AnyAsync(t => t.Key == treatmentKey);
        Assert.True(treatmentSaved, "Treatment should be saved");

        var setSaved = await verifyDb.Sets.AnyAsync(s => s.Code == setCode);
        Assert.True(setSaved, "Set should be saved");

        var cardSaved = await verifyDb.Cards.AnyAsync(c => c.Identifier == cardId);
        Assert.True(cardSaved, "Card should be saved");

        var contentVersion = packageManifest.ContentVersions["cards"].Version;
        var updateVersion = await verifyDb.UpdateVersions
            .FirstOrDefaultAsync(u => u.ContentVersion == contentVersion);
        Assert.NotNull(updateVersion);
    }

    [Fact]
    public async Task ApplyContentUpdate_UpsertsTreatment_WhenAlreadyExists()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var taxonomy = new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance);
        var verifier = new PackageVerifier();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, verifier, NullLogger<ContentUpdateApplicator>.Instance);

        var treatmentKey = $"upsert-{Guid.NewGuid():N}".Substring(0, 14);
        var setCode = $"u{Guid.NewGuid():N}".Substring(0, 4);

        var sets = new List<SetDto>
        {
            new() { Code = setCode, Name = "Upsert Test Set", TotalCards = 0 }
        };

        // First apply - create the treatment
        var (package1, manifest1) = PackageBuilder.Build(
            treatments: new List<TreatmentDto>
            {
                new() { Key = treatmentKey, DisplayName = "Original Name", SortOrder = 1 }
            },
            sets: sets);
        await applicator.ApplyContentUpdateAsync(package1, manifest1, CancellationToken.None);

        // Second apply - update the treatment display name
        await using var db2 = _fixture.CreateContext();
        var taxonomy2 = new SealedTaxonomyRepository(db2, NullLogger<SealedTaxonomyRepository>.Instance);
        var applicator2 = new ContentUpdateApplicator(
            db2, imageStore, taxonomy2, verifier, NullLogger<ContentUpdateApplicator>.Instance);

        var (package2, manifest2) = PackageBuilder.Build(
            treatments: new List<TreatmentDto>
            {
                new() { Key = treatmentKey, DisplayName = "Updated Name", SortOrder = 2 }
            },
            sets: sets);
        await applicator2.ApplyContentUpdateAsync(package2, manifest2, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();
        var treatment = await verifyDb.Treatments.FindAsync(treatmentKey);
        Assert.NotNull(treatment);
        Assert.Equal("Updated Name", treatment.DisplayName);
        Assert.Equal(2, treatment.SortOrder);
    }
}
