using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

// The treatments reference table is shipped in the update package and versioned
// independently. Product must never hardcode treatment values, so these assert that new
// treatments arriving upstream are picked up and associated without a Product release.
[Trait("Category", "RequiresDocker")]
public class TreatmentIngestionTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public TreatmentIngestionTest(PostgreSqlFixture fixture) => _fixture = fixture;

    // The full reference table as of the upstream expansion: the three that were ever
    // applied in practice, the six premium ones that now carry cards for the first time,
    // and four brand new ones.
    private static List<TreatmentDto> FullReferenceTable() =>
    [
        new() { Key = "regular",        DisplayName = "Regular",        SortOrder = 0 },
        new() { Key = "foil",           DisplayName = "Foil",           SortOrder = 1 },
        new() { Key = "etched-foil",    DisplayName = "Etched Foil",    SortOrder = 2 },
        new() { Key = "serialized",     DisplayName = "Serialized",     SortOrder = 3 },
        new() { Key = "surge-foil",     DisplayName = "Surge Foil",     SortOrder = 4 },
        new() { Key = "fracture-foil",  DisplayName = "Fracture Foil",  SortOrder = 5 },
        new() { Key = "textured-foil",  DisplayName = "Textured Foil",  SortOrder = 6 },
        new() { Key = "galaxy-foil",    DisplayName = "Galaxy Foil",    SortOrder = 7 },
        new() { Key = "gilded-foil",    DisplayName = "Gilded Foil",    SortOrder = 8 },
        new() { Key = "halo-foil",      DisplayName = "Halo Foil",      SortOrder = 9 },
        new() { Key = "ripple-foil",    DisplayName = "Ripple Foil",    SortOrder = 10 },
        new() { Key = "confetti-foil",  DisplayName = "Confetti Foil",  SortOrder = 11 },
        new() { Key = "rainbow-foil",   DisplayName = "Rainbow Foil",   SortOrder = 12 },
        new() { Key = "artist-proof",   DisplayName = "Artist Proof",   SortOrder = 13 },
        new() { Key = "promo",          DisplayName = "Promo",          SortOrder = 14 },
    ];

    private ContentUpdateApplicator Build(CountOrSell.Data.AppDbContext db)
        => new(db, new NoOpImageStore(),
            new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance),
            new PackageVerifier(), new NoOpHttpClientFactory(), new StubTreatmentValidator(),
            NullLogger<ContentUpdateApplicator>.Instance);

    [Fact]
    public async Task Every_Treatment_In_The_Package_Is_Ingested_With_Its_Shipped_Display_Name()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db);

        var (stream, manifest) = PackageBuilder.Build(treatments: FullReferenceTable());
        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var stored = await verify.Treatments.ToDictionaryAsync(t => t.Key, t => t);

        foreach (var expected in FullReferenceTable())
        {
            Assert.True(stored.ContainsKey(expected.Key), $"treatment {expected.Key} was not ingested");
            // Display name comes from the package, not from any mapping in this build.
            Assert.Equal(expected.DisplayName, stored[expected.Key].DisplayName);
            Assert.Equal(expected.SortOrder, stored[expected.Key].SortOrder);
        }
    }

    [Fact]
    public async Task A_Treatment_This_Build_Has_Never_Seen_Is_Ingested_And_Named_From_The_Package()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db);

        // A treatment key that exists nowhere in Product source. Nothing may special-case it.
        var invented = "prismatic-" + Guid.NewGuid().ToString("N")[..6];
        var table = FullReferenceTable();
        table.Add(new TreatmentDto { Key = invented, DisplayName = "Prismatic Overlay", SortOrder = 99 });

        var (stream, manifest) = PackageBuilder.Build(treatments: table);
        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var stored = await verify.Treatments.FirstOrDefaultAsync(t => t.Key == invented);

        Assert.NotNull(stored);
        Assert.Equal("Prismatic Overlay", stored!.DisplayName);
        Assert.Equal(99, stored.SortOrder);
    }

    [Fact]
    public async Task A_FoilOnly_Serialized_Card_Records_Exactly_Its_Two_Treatments()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db);

        // Modelled on BRR "64z" (Adaptive Automaton): a serialized printing that exists only
        // in foil, so it carries {foil, serialized} and no regular. The picker enumerates
        // from this association, so Regular must not appear.
        var setCode = "t" + Guid.NewGuid().ToString("N")[..3];
        var identifier = setCode + "064z";

        var (stream, manifest) = PackageBuilder.Build(
            treatments: FullReferenceTable(),
            sets: [new() { Code = setCode, Name = "Serialized Test Set" }],
            cards:
            [
                new()
                {
                    Identifier = identifier, SetCode = setCode, Name = "Adaptive Automaton",
                    Treatments = ["foil", "serialized"]
                }
            ]);

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var card = await verify.Cards.FirstAsync(c => c.Identifier == identifier);
        var valid = card.ValidTreatments!.Split(',');

        Assert.Equal(2, valid.Length);
        Assert.Contains("foil", valid);
        Assert.Contains("serialized", valid);
        Assert.DoesNotContain("regular", valid);
    }

    [Fact]
    public async Task A_Card_May_Carry_Any_Of_The_Newly_Flowing_Premium_Treatments()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db);

        var setCode = "p" + Guid.NewGuid().ToString("N")[..3];
        var premium = new[]
        {
            "serialized", "surge-foil", "fracture-foil", "textured-foil",
            "galaxy-foil", "gilded-foil", "halo-foil", "ripple-foil",
            "confetti-foil", "rainbow-foil"
        };

        var cards = premium.Select((t, i) => new CardDto
        {
            Identifier = $"{setCode}{i + 1:D3}",
            SetCode = setCode,
            Name = $"Premium {t}",
            Treatments = [t]
        }).ToList();

        var (stream, manifest) = PackageBuilder.Build(
            treatments: FullReferenceTable(),
            sets: [new() { Code = setCode, Name = "Premium Test Set" }],
            cards: cards);

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        foreach (var (treatment, i) in premium.Select((t, i) => (t, i)))
        {
            var card = await verify.Cards.FirstAsync(c => c.Identifier == $"{setCode}{i + 1:D3}");
            Assert.Equal(treatment, card.ValidTreatments);
        }
    }

    [Fact]
    public async Task A_Treatments_Only_Update_Applies_Without_Card_Or_Set_Data()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db);

        // A treatment-version bump on its own: treatments.json changes, nothing else ships.
        var renamed = FullReferenceTable();
        var target = renamed.First(t => t.Key == "galaxy-foil");
        target.DisplayName = "Galaxy Foil (Revised)";
        target.SortOrder = 42;

        var (stream, manifest) = PackageBuilder.Build(treatments: renamed);
        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var stored = await verify.Treatments.FirstAsync(t => t.Key == "galaxy-foil");

        // The rename is applied rather than ignored, and nothing else in ingestion breaks.
        Assert.Equal("Galaxy Foil (Revised)", stored.DisplayName);
        Assert.Equal(42, stored.SortOrder);
    }
}
