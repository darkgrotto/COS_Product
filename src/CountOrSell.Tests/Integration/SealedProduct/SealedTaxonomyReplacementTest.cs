using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using CountOrSell.Tests.Integration.Updates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.SealedProduct;

[Trait("Category", "RequiresDocker")]
public class SealedTaxonomyReplacementTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public SealedTaxonomyReplacementTest(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private ContentUpdateApplicator CreateApplicator(AppDbContext db) =>
        new(db, new NoOpImageStore(),
            new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance),
            new PackageVerifier(),
            NullLogger<ContentUpdateApplicator>.Instance);

    [Fact]
    public async Task ReplaceTaxonomy_InsertsNewCategoriesAndSubTypes_OrderedBySortOrder()
    {
        await using var db = _fixture.CreateContext();
        var applicator = CreateApplicator(db);

        var setCode = $"r{Guid.NewGuid():N}".Substring(0, 4);

        var taxonomy = new TaxonomyDto
        {
            Version = "1.0.0",
            Categories = new List<SealedProductCategoryDto>
            {
                new() { Slug = $"cat-b-{Guid.NewGuid():N}".Substring(0, 12), DisplayName = "Category B", SortOrder = 2 },
                new() { Slug = $"cat-a-{Guid.NewGuid():N}".Substring(0, 12), DisplayName = "Category A", SortOrder = 1 }
            }
        };

        var sets = new List<SetDto> { new() { Code = setCode, Name = "Tax Test Set", TotalCards = 0 } };
        var (pkg, manifest) = PackageBuilder.Build(sets: sets, taxonomy: taxonomy);
        await applicator.ApplyContentUpdateAsync(pkg, manifest, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();
        var taxonomyRepo = new SealedTaxonomyRepository(verifyDb, NullLogger<SealedTaxonomyRepository>.Instance);
        var categories = await taxonomyRepo.GetAllCategoriesAsync();

        Assert.Contains(categories, c => c.DisplayName == "Category A");
        Assert.Contains(categories, c => c.DisplayName == "Category B");

        var taxCats = categories.Where(c => c.DisplayName is "Category A" or "Category B").OrderBy(c => c.SortOrder).ToList();
        Assert.Equal("Category A", taxCats[0].DisplayName);
        Assert.Equal("Category B", taxCats[1].DisplayName);
    }

    [Fact]
    public async Task ReplaceTaxonomy_RemovesOldCategories_AndNullsOrphanedInventoryEntries()
    {
        await using var setupDb = _fixture.CreateContext();
        var setupApplicator = CreateApplicator(setupDb);

        var catSlugOld = $"cat-old-{Guid.NewGuid():N}".Substring(0, 16);
        var catSlugNew = $"cat-new-{Guid.NewGuid():N}".Substring(0, 16);
        var setCode = $"n{Guid.NewGuid():N}".Substring(0, 4);

        var sets = new List<SetDto> { new() { Code = setCode, Name = "Rm Tax Set", TotalCards = 0 } };

        // First apply: create old category
        var (pkg1, manifest1) = PackageBuilder.Build(
            sets: sets,
            taxonomy: new TaxonomyDto
            {
                Version = "1.0.0",
                Categories = new List<SealedProductCategoryDto>
                {
                    new() { Slug = catSlugOld, DisplayName = "Old Cat", SortOrder = 1 }
                }
            });
        await setupApplicator.ApplyContentUpdateAsync(pkg1, manifest1, CancellationToken.None);

        // Create a user with an inventory entry using the old category
        await using var db2 = _fixture.CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"taxtest-{Guid.NewGuid():N}",
            DisplayName = "Tax Test User",
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db2.Users.Add(user);
        var entry = new SealedInventoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProductIdentifier = "orphan-test-product",
            Quantity = 1,
            Condition = CardCondition.NM,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 10m,
            CategorySlug = catSlugOld,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db2.SealedInventoryEntries.Add(entry);
        await db2.SaveChangesAsync();
        var entryId = entry.Id;

        // Second apply: replace with new category (old category removed)
        await using var db3 = _fixture.CreateContext();
        var applicator2 = CreateApplicator(db3);
        var (pkg2, manifest2) = PackageBuilder.Build(
            sets: sets,
            taxonomy: new TaxonomyDto
            {
                Version = "1.0.1",
                Categories = new List<SealedProductCategoryDto>
                {
                    new() { Slug = catSlugNew, DisplayName = "New Cat", SortOrder = 1 }
                }
            });
        await applicator2.ApplyContentUpdateAsync(pkg2, manifest2, CancellationToken.None);

        // Verify inventory entry has been nulled
        await using var verifyDb = _fixture.CreateContext();
        var updatedEntry = await verifyDb.SealedInventoryEntries.FindAsync(entryId);
        Assert.NotNull(updatedEntry);
        Assert.Null(updatedEntry.CategorySlug);
        Assert.Null(updatedEntry.SubTypeSlug);

        var oldCatExists = await verifyDb.SealedProductCategories.AnyAsync(c => c.Slug == catSlugOld);
        Assert.False(oldCatExists);

        var newCatExists = await verifyDb.SealedProductCategories.AnyAsync(c => c.Slug == catSlugNew);
        Assert.True(newCatExists);
    }

    [Fact]
    public async Task ReplaceTaxonomy_RemovesOldSubType_AndNullsOnlySubTypeSlug_WhenCategoryRemains()
    {
        await using var setupDb = _fixture.CreateContext();
        var setupApplicator = CreateApplicator(setupDb);

        var catSlug = $"cat-keep-{Guid.NewGuid():N}".Substring(0, 16);
        var subSlugOld = $"st-old-{Guid.NewGuid():N}".Substring(0, 14);
        var subSlugNew = $"st-new-{Guid.NewGuid():N}".Substring(0, 14);
        var setCode = $"s{Guid.NewGuid():N}".Substring(0, 4);

        var sets = new List<SetDto> { new() { Code = setCode, Name = "Sub Rm Set", TotalCards = 0 } };

        var (pkg1, manifest1) = PackageBuilder.Build(
            sets: sets,
            taxonomy: new TaxonomyDto
            {
                Version = "1.0.0",
                Categories = new List<SealedProductCategoryDto>
                {
                    new()
                    {
                        Slug = catSlug,
                        DisplayName = "Keep Cat",
                        SortOrder = 1,
                        SubTypes = new List<SealedProductSubTypeDto>
                        {
                            new() { Slug = subSlugOld, DisplayName = "Old Sub", SortOrder = 1 }
                        }
                    }
                }
            });
        await setupApplicator.ApplyContentUpdateAsync(pkg1, manifest1, CancellationToken.None);

        // Create inventory entry with the old sub-type
        await using var db2 = _fixture.CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"taxtest2-{Guid.NewGuid():N}",
            DisplayName = "Tax Test User 2",
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db2.Users.Add(user);
        var entry = new SealedInventoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProductIdentifier = "sub-orphan-product",
            Quantity = 1,
            Condition = CardCondition.NM,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 5m,
            CategorySlug = catSlug,
            SubTypeSlug = subSlugOld,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db2.SealedInventoryEntries.Add(entry);
        await db2.SaveChangesAsync();
        var entryId = entry.Id;

        // Second apply: category stays, old sub-type replaced with new sub-type
        await using var db3 = _fixture.CreateContext();
        var applicator2 = CreateApplicator(db3);
        var (pkg2, manifest2) = PackageBuilder.Build(
            sets: sets,
            taxonomy: new TaxonomyDto
            {
                Version = "1.0.1",
                Categories = new List<SealedProductCategoryDto>
                {
                    new()
                    {
                        Slug = catSlug,
                        DisplayName = "Keep Cat",
                        SortOrder = 1,
                        SubTypes = new List<SealedProductSubTypeDto>
                        {
                            new() { Slug = subSlugNew, DisplayName = "New Sub", SortOrder = 1 }
                        }
                    }
                }
            });
        await applicator2.ApplyContentUpdateAsync(pkg2, manifest2, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();
        var updatedEntry = await verifyDb.SealedInventoryEntries.FindAsync(entryId);
        Assert.NotNull(updatedEntry);
        Assert.Equal(catSlug, updatedEntry.CategorySlug);
        Assert.Null(updatedEntry.SubTypeSlug);

        var catExists = await verifyDb.SealedProductCategories.AnyAsync(c => c.Slug == catSlug);
        Assert.True(catExists);

        var oldSubExists = await verifyDb.SealedProductSubTypes.AnyAsync(s => s.Slug == subSlugOld);
        Assert.False(oldSubExists);
        var newSubExists = await verifyDb.SealedProductSubTypes.AnyAsync(s => s.Slug == subSlugNew);
        Assert.True(newSubExists);
    }

    [Fact]
    public async Task SealedInventory_CanBeCreated_WithValidCategoryAndSubType()
    {
        await using var db = _fixture.CreateContext();

        var catSlug = $"cat-inv-{Guid.NewGuid():N}".Substring(0, 15);
        var subSlug = $"st-inv-{Guid.NewGuid():N}".Substring(0, 14);

        db.SealedProductCategories.Add(new SealedProductCategory
        {
            Slug = catSlug,
            DisplayName = "Inv Cat",
            SortOrder = 1
        });
        db.SealedProductSubTypes.Add(new SealedProductSubType
        {
            Slug = subSlug,
            CategorySlug = catSlug,
            DisplayName = "Inv Sub",
            SortOrder = 1
        });
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"inv-cat-{Guid.NewGuid():N}",
            DisplayName = "Inv Cat User",
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var entry = new SealedInventoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProductIdentifier = "cat-test-product",
            Quantity = 1,
            Condition = CardCondition.NM,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 20m,
            CategorySlug = catSlug,
            SubTypeSlug = subSlug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SealedInventoryEntries.Add(entry);
        await db.SaveChangesAsync();

        await using var verifyDb = _fixture.CreateContext();
        var saved = await verifyDb.SealedInventoryEntries.FindAsync(entry.Id);
        Assert.NotNull(saved);
        Assert.Equal(catSlug, saved.CategorySlug);
        Assert.Equal(subSlug, saved.SubTypeSlug);
    }
}
