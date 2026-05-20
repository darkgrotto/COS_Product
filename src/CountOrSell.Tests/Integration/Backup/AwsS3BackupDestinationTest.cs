using CountOrSell.Api.Services.Destinations;
using Testcontainers.LocalStack;
using Xunit;

namespace CountOrSell.Tests.Integration.Backup;

public class AwsS3BackupDestinationTest : IAsyncLifetime
{
    private readonly LocalStackContainer _localstack = new LocalStackBuilder()
        .WithImage("localstack/localstack:3")
        .Build();

    private string _serviceUrl = string.Empty;

    public async Task InitializeAsync()
    {
        await _localstack.StartAsync();
        _serviceUrl = _localstack.GetConnectionString();
    }

    public async Task DisposeAsync() => await _localstack.DisposeAsync();

    private AwsS3BackupDestination CreateDestination(string bucket = "cos-test")
        => new(
            bucket,
            "us-east-1",
            accessKey: "test",
            secretKey: "test",
            serviceUrl: _serviceUrl,
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
