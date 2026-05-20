using CountOrSell.Api.Services.Destinations;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace CountOrSell.Tests.Integration.Backup;

public class GcpStorageBackupDestinationTest : IAsyncLifetime
{
    // fake-gcs-server's resumable-upload protocol embeds the server's `-public-host`
    // value in the Location header it returns to the client. With a random Testcontainers
    // mapping, the client cannot guess that value, so we pin a fixed host port to keep
    // the two in sync. The port is unusual enough to avoid common conflicts; the test
    // fixture will fail clearly if it is already in use.
    private const int FakeGcsHostPort = 14443;
    private const int FakeGcsContainerPort = 4443;

    private readonly IContainer _fakeGcs = new ContainerBuilder()
        .WithImage("fsouza/fake-gcs-server:1.49.2")
        // -external-url controls the host:port embedded in Location headers for
        // resumable uploads; without it the server returns `http://0.0.0.0:4443/...`
        // (its bind address), which the .NET SDK then tries to follow and fails to
        // resolve. -public-host serves the same purpose for selfLink/mediaLink fields
        // in JSON responses.
        .WithCommand("-scheme", "http",
                     "-public-host", $"localhost:{FakeGcsHostPort}",
                     "-external-url", $"http://localhost:{FakeGcsHostPort}")
        .WithPortBinding(FakeGcsHostPort, FakeGcsContainerPort)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(FakeGcsContainerPort))
        .Build();

    private string _endpoint = string.Empty;

    public async Task InitializeAsync()
    {
        await _fakeGcs.StartAsync();
        _endpoint = $"http://localhost:{FakeGcsHostPort}";
    }

    public async Task DisposeAsync() => await _fakeGcs.DisposeAsync();

    private GcpStorageBackupDestination CreateDestination(string bucket = "cos-test")
        => new(
            bucket,
            projectId: "test-project",
            credentialsJson: null,
            endpoint: _endpoint,
            label: "Test");

    [Fact]
    public async Task TestConnection_Creates_Bucket_And_Returns_True()
    {
        var dest = CreateDestination("connectivity-test");

        var ok = await dest.TestConnectionAsync(CancellationToken.None);

        Assert.True(ok);
    }

    [Fact]
    public async Task Write_Then_Read_Roundtrips_Payload()
    {
        var dest = CreateDestination("roundtrip");
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        using (var input = new MemoryStream(payload))
            await dest.WriteAsync("backup-001.zip", input, CancellationToken.None);

        await using var stream = await dest.ReadAsync("backup-001.zip", CancellationToken.None);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);

        Assert.Equal(payload, copy.ToArray());
    }

    [Fact]
    public async Task ListFiles_Returns_Only_Zip_Objects()
    {
        var dest = CreateDestination("list-test");
        var payload = new byte[] { 0x42 };

        using (var s1 = new MemoryStream(payload))
            await dest.WriteAsync("a.zip", s1, CancellationToken.None);
        using (var s2 = new MemoryStream(payload))
            await dest.WriteAsync("b.zip", s2, CancellationToken.None);
        using (var s3 = new MemoryStream(payload))
            await dest.WriteAsync("notes.txt", s3, CancellationToken.None);

        var files = await dest.ListFilesAsync(CancellationToken.None);

        Assert.Equal(2, files.Count);
        Assert.Contains("a.zip", files);
        Assert.Contains("b.zip", files);
    }

    [Fact]
    public async Task Delete_Removes_Object()
    {
        var dest = CreateDestination("delete-test");
        using (var input = new MemoryStream(new byte[] { 9, 9 }))
            await dest.WriteAsync("doomed.zip", input, CancellationToken.None);

        await dest.DeleteAsync("doomed.zip", CancellationToken.None);

        var files = await dest.ListFilesAsync(CancellationToken.None);
        Assert.DoesNotContain("doomed.zip", files);
    }

    [Fact]
    public async Task Delete_Missing_Object_Does_Not_Throw()
    {
        var dest = CreateDestination("delete-missing");
        await dest.TestConnectionAsync(CancellationToken.None);

        await dest.DeleteAsync("never-existed.zip", CancellationToken.None);
    }

    [Theory]
    [InlineData("../escape.zip")]
    [InlineData("nested/path.zip")]
    [InlineData("with\\backslash.zip")]
    public async Task Write_Rejects_Path_Traversal_FileName(string fileName)
    {
        var dest = CreateDestination("traversal-test");
        using var stream = new MemoryStream(new byte[] { 1 });

        await Assert.ThrowsAsync<ArgumentException>(
            () => dest.WriteAsync(fileName, stream, CancellationToken.None));
    }

    [Fact]
    public async Task ListFiles_On_Missing_Bucket_Returns_Empty()
    {
        var dest = CreateDestination("never-created");

        var files = await dest.ListFilesAsync(CancellationToken.None);

        Assert.Empty(files);
    }
}
