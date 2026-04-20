using CountOrSell.Api.Services;
using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using System.Net.Http;

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
        var taxonomy = new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance);
        var verifier = new PackageVerifier();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, verifier, new NoOpHttpClientFactory(), NullLogger<ContentUpdateApplicator>.Instance);

        // Build a package with treatments but cards referencing a set that does not exist
        var (packageStream, packageManifest) = PackageBuilder.Build(
            treatments: new List<TreatmentDto>
            {
                new() { Key = "regular", DisplayName = "Regular", SortOrder = 0 }
            },
            // No sets - cards will reference a non-existent set_code
            cards: new List<CardDto>
            {
                new() { Identifier = "tst001", SetCode = "tst", Name = "Test Card" }
            });

        // Should throw due to FK violation on cards.set_code -> sets.code
        await Assert.ThrowsAnyAsync<Exception>(() =>
            applicator.ApplyContentUpdateAsync(packageStream, packageManifest, "http://localhost/test/", CancellationToken.None));

        // Transaction should have rolled back - the update version must NOT be recorded
        await using var verifyDb = _fixture.CreateContext();
        var contentVersion = packageManifest.ContentVersions["cards"].Version;
        var updateVersionExists = await verifyDb.UpdateVersions
            .AnyAsync(u => u.ContentVersion == contentVersion);
        Assert.False(updateVersionExists);
    }

    [Fact]
    public async Task ApplyContentUpdate_RollsBack_WhenCardReferencesNonExistentSet_UniqueTreatmentKey()
    {
        await using var db = _fixture.CreateContext();
        var imageStore = new NoOpImageStore();
        var taxonomy = new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance);
        var verifier = new PackageVerifier();
        var applicator = new ContentUpdateApplicator(
            db, imageStore, taxonomy, verifier, new NoOpHttpClientFactory(), NullLogger<ContentUpdateApplicator>.Instance);

        var uniqueTreatmentKey = $"rollback-test-{Guid.NewGuid():N}";
        var (packageStream, packageManifest) = PackageBuilder.Build(
            treatments: new List<TreatmentDto>
            {
                new() { Key = uniqueTreatmentKey, DisplayName = "Rollback Test Treatment", SortOrder = 99 }
            },
            cards: new List<CardDto>
            {
                new() { Identifier = "zz9001", SetCode = "zz9", Name = "Rollback Test Card" }
            });

        await Assert.ThrowsAnyAsync<Exception>(() =>
            applicator.ApplyContentUpdateAsync(packageStream, packageManifest, "http://localhost/test/", CancellationToken.None));

        // Verify the treatment was NOT persisted (transaction rolled back)
        await using var verifyDb = _fixture.CreateContext();
        var treatmentExists = await verifyDb.Treatments.AnyAsync(t => t.Key == uniqueTreatmentKey);
        Assert.False(treatmentExists);
    }
}

// Test double - no-op HttpClientFactory for tests that do not fetch images.
internal sealed class NoOpHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
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

    public Task DeleteImageAsync(string relativePath, CancellationToken ct)
        => Task.CompletedTask;

    public Task<bool> HasImagesAsync(CancellationToken ct)
        => Task.FromResult(false);
}
