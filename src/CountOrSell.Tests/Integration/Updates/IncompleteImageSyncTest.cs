using System.Net;
using System.Net.Http;
using CountOrSell.Api.Services;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

// A full package lists ~101,000 images. If the request budget runs out partway, the update
// used to report plain success while leaving thousands of cards without artwork. These cover
// the applicator reporting the shortfall instead of swallowing it.
[Trait("Category", "RequiresDocker")]
public class IncompleteImageSyncTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public IncompleteImageSyncTest(PostgreSqlFixture fixture) => _fixture = fixture;

    private const string ImageKey = "images/sets/tst/tst001.jpg";
    private const string BaseUrl = "https://packages.countorsell.com/p/";

    private static (System.IO.MemoryStream stream, PackageManifest manifest) MinimalPackageWithOneImage()
    {
        var (stream, manifest) = PackageBuilder.Build(
            treatments: [new() { Key = "regular", DisplayName = "Regular", SortOrder = 0 }],
            sets: [new() { Code = "tst", Name = "Test Set" }],
            cards: [new() { Identifier = "tst001", SetCode = "tst", Name = "Test Card" }]);

        // Listed in the signed manifest, served as a loose blob - the shape every package
        // currently published takes.
        manifest.Checksums[ImageKey] =
            "sha256:0000000000000000000000000000000000000000000000000000000000000000";
        return (stream, manifest);
    }

    private ContentUpdateApplicator Build(CountOrSell.Data.AppDbContext db, HttpStatusCode status)
        => new(db, new NoOpImageStore(),
            new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance),
            new PackageVerifier(), new FixedStatusHttpClientFactory(status),
            new StubTreatmentValidator(), NullLogger<ContentUpdateApplicator>.Instance);

    [Fact]
    public async Task RateLimitedImages_AreReportedAsAShortfall_NotSwallowed()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db, HttpStatusCode.TooManyRequests);
        var (stream, manifest) = MinimalPackageWithOneImage();

        var outcome = await applicator.ApplyContentUpdateAsync(
            stream, manifest, BaseUrl, CancellationToken.None);

        Assert.False(outcome.IsComplete);
        Assert.Equal(1, outcome.Listed);
        Assert.Equal(0, outcome.Saved);
        Assert.Equal(1, outcome.Missing);
        // Rate limiting is distinguished from a generic failure so the message can point at
        // the request budget rather than blaming the package.
        Assert.Equal(1, outcome.RateLimited);
        Assert.Contains("429", outcome.Explain());
    }

    [Fact]
    public async Task OtherFetchFailures_AreReportedWithoutClaimingRateLimiting()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db, HttpStatusCode.InternalServerError);
        var (stream, manifest) = MinimalPackageWithOneImage();

        var outcome = await applicator.ApplyContentUpdateAsync(
            stream, manifest, BaseUrl, CancellationToken.None);

        Assert.False(outcome.IsComplete);
        Assert.Equal(1, outcome.Failed);
        Assert.Equal(0, outcome.RateLimited);
        Assert.DoesNotContain("429", outcome.Explain());
    }

    [Fact]
    public async Task PackageWithNoImages_ReportsComplete_NotAShortfall()
    {
        await using var db = _fixture.CreateContext();
        var applicator = Build(db, HttpStatusCode.TooManyRequests);

        // Deltas carry no images at all; that is normal and must not read as a failure.
        var (stream, manifest) = PackageBuilder.Build(
            treatments: [new() { Key = "regular", DisplayName = "Regular", SortOrder = 0 }],
            sets: [new() { Code = "tst", Name = "Test Set" }]);

        var outcome = await applicator.ApplyContentUpdateAsync(
            stream, manifest, BaseUrl, CancellationToken.None);

        Assert.True(outcome.IsComplete);
        Assert.Equal(0, outcome.Listed);
    }
}

// Returns the same status for every request, so a test can drive the failure branch.
internal sealed class FixedStatusHttpClientFactory : IHttpClientFactory
{
    private readonly HttpStatusCode _status;
    public FixedStatusHttpClientFactory(HttpStatusCode status) => _status = status;

    public HttpClient CreateClient(string name) => new(new Handler(_status));

    private sealed class Handler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public Handler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status));
    }
}
