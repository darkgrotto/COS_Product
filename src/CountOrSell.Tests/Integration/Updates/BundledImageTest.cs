using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using CountOrSell.Api.Services;
using CountOrSell.Data.Images;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Dtos.Packages;
using CountOrSell.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

// A full package lists roughly 98,000 images. Historically every one was fetched as a
// separate HTTP request against the package base URL, because the published ZIP carried
// metadata only. These tests cover reading images out of the ZIP when they are bundled,
// while keeping the per-file fallback for packages that are not.
[Trait("Category", "RequiresDocker")]
public class BundledImageTest : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public BundledImageTest(PostgreSqlFixture fixture) => _fixture = fixture;

    private const string CardImageKey = "images/sets/tst/tst001.jpg";
    private const string SealedImageKey = "images/sealed/tstp001.jpg";

    private static byte[] Jpeg(byte marker) => [0xFF, 0xD8, 0xFF, marker, 0x00, 0x10, marker];

    private static (RecordingImageStore store, RecordingHttpClientFactory http, ContentUpdateApplicator applicator)
        BuildApplicator(PostgreSqlFixture fixture, CountOrSell.Data.AppDbContext db,
            IDictionary<string, byte[]>? httpServes = null)
    {
        var store = new RecordingImageStore();
        var http = new RecordingHttpClientFactory(httpServes);
        var applicator = new ContentUpdateApplicator(
            db, store,
            new SealedTaxonomyRepository(db, NullLogger<SealedTaxonomyRepository>.Instance),
            new PackageVerifier(), http, new StubTreatmentValidator(),
            NullLogger<ContentUpdateApplicator>.Instance);
        return (store, http, applicator);
    }

    private static (List<TreatmentDto> treatments, List<SetDto> sets, List<CardDto> cards) MinimalContent()
        => (
            [new() { Key = "regular", DisplayName = "Regular", SortOrder = 0 }],
            [new() { Code = "tst", Name = "Test Set" }],
            [new() { Identifier = "tst001", SetCode = "tst", Name = "Test Card" }]
        );

    [Fact]
    public async Task BundledImages_AreReadFromThePackage_WithoutAnyHttpFetch()
    {
        await using var db = _fixture.CreateContext();
        var (store, http, applicator) = BuildApplicator(_fixture, db);
        var (treatments, sets, cards) = MinimalContent();

        var image = Jpeg(0xE0);
        var (stream, manifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards,
            bundledImages: new Dictionary<string, byte[]> { [CardImageKey] = image });

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        Assert.Equal(image, store.Saved["sets/tst/tst001.jpg"]);
        Assert.Empty(http.RequestedUrls);
    }

    [Fact]
    public async Task UnbundledImages_StillFallBackToPerFileHttpFetch()
    {
        await using var db = _fixture.CreateContext();
        var image = Jpeg(0xE1);
        var (store, http, applicator) = BuildApplicator(
            _fixture, db,
            new Dictionary<string, byte[]> { ["https://packages.countorsell.com/p/" + CardImageKey] = image });
        var (treatments, sets, cards) = MinimalContent();

        var (stream, manifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards,
            unbundledImages: new Dictionary<string, byte[]> { [CardImageKey] = image });

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        Assert.Equal(image, store.Saved["sets/tst/tst001.jpg"]);
        Assert.Contains("https://packages.countorsell.com/p/" + CardImageKey, http.RequestedUrls);
    }

    [Fact]
    public async Task MixedPackage_TakesBundledFromZip_AndFetchesOnlyTheRest()
    {
        await using var db = _fixture.CreateContext();
        var bundled = Jpeg(0xE2);
        var loose = Jpeg(0xE3);
        var (store, http, applicator) = BuildApplicator(
            _fixture, db,
            new Dictionary<string, byte[]> { ["https://packages.countorsell.com/p/" + SealedImageKey] = loose });
        var (treatments, sets, cards) = MinimalContent();

        var (stream, manifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards,
            bundledImages: new Dictionary<string, byte[]> { [CardImageKey] = bundled },
            unbundledImages: new Dictionary<string, byte[]> { [SealedImageKey] = loose });

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        Assert.Equal(bundled, store.Saved["sets/tst/tst001.jpg"]);
        Assert.Equal(loose, store.Saved["sealed/tstp001.jpg"]);

        // Only the image absent from the ZIP costs a request.
        Assert.Single(http.RequestedUrls);
        Assert.Contains(SealedImageKey, http.RequestedUrls[0]);
    }

    [Fact]
    public async Task BundledImage_FailingItsChecksum_IsNotSaved()
    {
        await using var db = _fixture.CreateContext();
        var (store, http, applicator) = BuildApplicator(_fixture, db);
        var (treatments, sets, cards) = MinimalContent();

        var (stream, manifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards,
            bundledImages: new Dictionary<string, byte[]> { [CardImageKey] = Jpeg(0xE4) });

        // Point the signed checksum at different content. Reading from the ZIP must not
        // become a way to skip verification - the package ZIP checksum covers the archive,
        // not the individual entry the store ends up holding.
        manifest.Checksums[CardImageKey] =
            "sha256:0000000000000000000000000000000000000000000000000000000000000000";

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        Assert.False(store.Saved.ContainsKey("sets/tst/tst001.jpg"));
        // Image failures are best-effort, so the update itself still succeeded.
    }

    [Fact]
    public async Task BundledImage_WithATraversingManifestKey_IsRejectedBeforeAnyLookup()
    {
        await using var db = _fixture.CreateContext();
        var (store, http, applicator) = BuildApplicator(_fixture, db);
        var (treatments, sets, cards) = MinimalContent();

        var (stream, manifest) = PackageBuilder.Build(
            treatments: treatments, sets: sets, cards: cards);

        // The key never matches the documented shape, so it is dropped before it can be
        // used as a ZIP entry name, a URL, or a store path.
        manifest.Checksums["images/../../etc/passwd"] =
            "sha256:0000000000000000000000000000000000000000000000000000000000000000";

        await applicator.ApplyContentUpdateAsync(stream, manifest, "https://packages.countorsell.com/p/", CancellationToken.None);

        Assert.Empty(store.Saved);
        Assert.Empty(http.RequestedUrls);
    }
}

// Records what the applicator stored, so tests can assert on bytes rather than call counts.
internal sealed class RecordingImageStore : IImageStore
{
    public ConcurrentDictionary<string, byte[]> Saved { get; } = new();

    public Task SaveImageAsync(string relativePath, byte[] data, CancellationToken ct)
    {
        Saved[relativePath] = data;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetImageAsync(string relativePath, CancellationToken ct)
        => Task.FromResult(Saved.TryGetValue(relativePath, out var d) ? d : null);

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct)
        => Task.FromResult(Saved.ContainsKey(relativePath));

    public Task DeleteImageAsync(string relativePath, CancellationToken ct)
    {
        Saved.TryRemove(relativePath, out _);
        return Task.CompletedTask;
    }

    public Task<bool> HasImagesAsync(CancellationToken ct) => Task.FromResult(!Saved.IsEmpty);
    public Task<int> PurgeSetImagesAsync(string setCode, CancellationToken ct) => Task.FromResult(0);
    public Task<int> PurgeSealedImagesAsync(CancellationToken ct) => Task.FromResult(0);
    public Task<int> PurgeAllImagesAsync(CancellationToken ct) => Task.FromResult(0);
    public Task<Dictionary<string, int>> GetImageCountsBySetAsync(CancellationToken ct)
        => Task.FromResult(new Dictionary<string, int>());
    public Task<int> GetSealedImageCountAsync(CancellationToken ct) => Task.FromResult(0);
}

// Serves a fixed map of URL -> bytes and records every URL requested, so a test can assert
// that bundling an image removed its request entirely.
internal sealed class RecordingHttpClientFactory : IHttpClientFactory
{
    private readonly IDictionary<string, byte[]> _serves;
    public List<string> RequestedUrls { get; } = [];

    public RecordingHttpClientFactory(IDictionary<string, byte[]>? serves)
        => _serves = serves ?? new Dictionary<string, byte[]>();

    public HttpClient CreateClient(string name) => new(new Handler(this));

    private sealed class Handler : HttpMessageHandler
    {
        private readonly RecordingHttpClientFactory _owner;
        public Handler(RecordingHttpClientFactory owner) => _owner = owner;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            lock (_owner.RequestedUrls) _owner.RequestedUrls.Add(url);

            if (_owner._serves.TryGetValue(url, out var bytes))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(bytes)
                });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
