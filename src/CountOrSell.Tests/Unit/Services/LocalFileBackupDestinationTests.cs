using CountOrSell.Api.Services.Destinations;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

public class LocalFileBackupDestinationTests : IDisposable
{
    private readonly string _tempBase;

    public LocalFileBackupDestinationTests()
    {
        _tempBase = Path.Combine(Path.GetTempPath(), "cos-local-dest-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempBase);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempBase, recursive: true); } catch { }
    }

    [Fact]
    public async Task WriteAsync_Refuses_Path_Traversal_Filename()
    {
        var dest = new LocalFileBackupDestination(_tempBase);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<ArgumentException>(
            () => dest.WriteAsync("../escaped.zip", stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_Refuses_Path_Traversal_Filename()
    {
        var dest = new LocalFileBackupDestination(_tempBase);

        await Assert.ThrowsAsync<ArgumentException>(
            () => dest.ReadAsync("../../etc/passwd", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Refuses_Absolute_Path()
    {
        var dest = new LocalFileBackupDestination(_tempBase);

        await Assert.ThrowsAsync<ArgumentException>(
            () => dest.DeleteAsync("/etc/passwd", CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_Accepts_Plain_Filename()
    {
        var dest = new LocalFileBackupDestination(_tempBase);
        var payload = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(payload);

        await dest.WriteAsync("regular.zip", stream, CancellationToken.None);

        var written = await File.ReadAllBytesAsync(Path.Combine(_tempBase, "regular.zip"));
        Assert.Equal(payload, written);
    }
}
