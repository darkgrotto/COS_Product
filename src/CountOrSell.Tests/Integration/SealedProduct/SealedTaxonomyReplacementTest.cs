using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
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
            NullLogger<ContentUpdateApplicator>.Instance);

    [Fact]
    public async Task ReplaceTaxonomy_InsertsNewCategoriesAndSubTypes_OrderedBySortOrder()
    {
        await using var db = _fixture.CreateContext();
        var applicator = CreateApplicator(db);

        var setCode = $"r{Guid.NewGuid():N}".Substring(0, 4);
        var cv = $"v-tax-{Guid.NewGuid():N}";

        var sealedCategories = new[]
        {
            new
            {
                slug = $"cat-b-{Guid.NewGuid():N}".Substring(0, 12),
                displayName = "Category B",
                sortOrder = 2,
                subTypes = Array.Empty<object>()
            },
            new
            {
                slug = $"cat-a-{Guid.NewGuid():N}".Substring(0, 12),
                displayName = "Category A",
                sortOrder = 1,
                subTypes = Array.Empty<object>()
            }
        };

        var sets = new[] { new { code = setCode, name = "Tax Test Set", totalCards = 0, releaseDate = (string?)null } };
        using var pkg = PackageBuilder.Build(sets: sets, sealedCategories: sealedCategories);
        await applicator.ApplyContentUpdateAsync(pkg, cv, CancellationToken.None);

        await using var verifyDb = _fixture.CreateContext();
        var taxonomy = new SealedTaxonomyRepository(verifyDb, NullLogger<SealedTaxonomyRepository>.Instance);
        var categories = await taxonomy.GetAllCategoriesAsync();

        // Both categories should exist
        Assert.Contains(categories, c => c.DisplayName == "Category A");
        Assert.Contains(categories, c => c.DisplayName == "Category B");

        // Should be ordered by sort_order
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
        var cv1 = $"v-tax-rm1-{Guid.NewGuid():N}";

        // First apply: create old category
        var categories1 = new[]
        {
            new { slug = catSlugOld, displayName = "Old Cat", sortOrder = 1, subTypes = Array.Empty<object>() }
        };
        var sets = new[] { new { code = setCode, name = "Rm Tax Set", totalCards = 0, releaseDate = (string?)null } };
        using var pkg1 = PackageBuilder.Build(sets: sets, sealedCategories: categories1);
        await setupApplicator.ApplyContentUpdateAsync(pkg1, cv1, CancellationToken.None);

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
        var categories2 = new[]
        {
            new { slug = catSlugNew, displayName = "New Cat", sortOrder = 1, subTypes = Array.Empty<object>() }
        };
        var cv2 = $"v-tax-rm2-{Guid.NewGuid():N}";
        using var pkg2 = PackageBuilder.Build(sets: sets, sealedCategories: categories2);
        await applicator2.ApplyContentUpdateAsync(pkg2, cv2, CancellationToken.None);

        // Verify inventory entry has been nulled
        await using var verifyDb = _fixture.CreateContext();
        var updatedEntry = await verifyDb.SealedInventoryEntries.FindAsync(entryId);
        Assert.NotNull(updatedEntry);
        Assert.Null(updatedEntry.CategorySlug);
        Assert.Null(updatedEntry.SubTypeSlug);

        // Verify old category is gone
        var oldCatExists = await verifyDb.SealedProductCategories.AnyAsync(c => c.Slug == catSlugOld);
        Assert.False(oldCatExists);

        // Verify new category is present
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
        var cv1 = $"v-stold-{Guid.NewGuid():N}";

        var categories1 = new[]
        {
            new
            {
                slug = catSlug,
                displayName = "Keep Cat",
                sortOrder = 1,
                subTypes = new[]
                {
                    new { slug = subSlugOld, categorySlug = catSlug, displayName = "Old Sub", sortOrder = 1 }
                }
            }
        };
        var sets = new[] { new { code = setCode, name = "Sub Rm Set", totalCards = 0, releaseDate = (string?)null } };
        using var pkg1 = PackageBuilder.Build(sets: sets, sealedCategories: categories1);
        await setupApplicator.ApplyContentUpdateAsync(pkg1, cv1, CancellationToken.None);

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
        var categories2 = new[]
        {
            new
            {
                slug = catSlug,
                displayName = "Keep Cat",
                sortOrder = 1,
                subTypes = new[]
                {
                    new { slug = subSlugNew, categorySlug = catSlug, displayName = "New Sub", sortOrder = 1 }
                }
            }
        };
        var cv2 = $"v-stnew-{Guid.NewGuid():N}";
        using var pkg2 = PackageBuilder.Build(sets: sets, sealedCategories: categories2);
        await applicator2.ApplyContentUpdateAsync(pkg2, cv2, CancellationToken.None);

        // Verify: category_slug preserved, sub_type_slug nulled
        await using var verifyDb = _fixture.CreateContext();
        var updatedEntry = await verifyDb.SealedInventoryEntries.FindAsync(entryId);
        Assert.NotNull(updatedEntry);
        Assert.Equal(catSlug, updatedEntry.CategorySlug);
        Assert.Null(updatedEntry.SubTypeSlug);

        // Category should still exist
        var catExists = await verifyDb.SealedProductCategories.AnyAsync(c => c.Slug == catSlug);
        Assert.True(catExists);

        // New sub-type should exist, old should not
        var oldSubExists = await verifyDb.SealedProductSubTypes.AnyAsync(s => s.Slug == subSlugOld);
        Assert.False(oldSubExists);
        var newSubExists = await verifyDb.SealedProductSubTypes.AnyAsync(s => s.Slug == subSlugNew);
        Assert.True(newSubExists);
    }

    [Fact]
    public async Task SealedInventory_CanBeCreated_WithValidCategoryAndSubType()
    {
        // This test verifies the schema constraint: category_slug FK and sub_type_slug FK
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
