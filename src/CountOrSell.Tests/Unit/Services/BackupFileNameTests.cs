using CountOrSell.Api.Services;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

public class BackupFileNameTests : IDisposable
{
    private readonly string _tempBase;

    public BackupFileNameTests()
    {
        _tempBase = Path.Combine(Path.GetTempPath(), "cos-backup-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempBase);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempBase, recursive: true); } catch { }
    }

    private static BackupRecord NewRecord(string label) => new()
    {
        Id = Guid.NewGuid(),
        Label = label,
        BackupType = BackupType.Scheduled,
        SchemaVersion = 1,
        CreatedAt = DateTime.UtcNow,
        IsAvailable = true
    };

    [Fact]
    public void For_Returns_Guid_Based_Name()
    {
        var record = NewRecord("any-label");
        Assert.Equal($"{record.Id}.zip", BackupFileName.For(record));
    }

    [Fact]
    public void TryResolvePath_Prefers_Guid_File_When_Both_Exist()
    {
        var record = NewRecord("legacy-label");
        File.WriteAllBytes(Path.Combine(_tempBase, BackupFileName.For(record)), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_tempBase, BackupFileName.LegacyFor(record)), new byte[] { 2 });

        Assert.True(BackupFileName.TryResolvePath(_tempBase, record, out var resolved));
        Assert.EndsWith(BackupFileName.For(record), resolved);
    }

    [Fact]
    public void TryResolvePath_Falls_Back_To_Legacy_Name()
    {
        var record = NewRecord("legacy-label");
        File.WriteAllBytes(Path.Combine(_tempBase, BackupFileName.LegacyFor(record)), new byte[] { 1 });

        Assert.True(BackupFileName.TryResolvePath(_tempBase, record, out var resolved));
        Assert.EndsWith(BackupFileName.LegacyFor(record), resolved);
    }

    [Fact]
    public void TryResolvePath_Returns_False_When_Neither_Exists()
    {
        var record = NewRecord("missing");
        Assert.False(BackupFileName.TryResolvePath(_tempBase, record, out var resolved));
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void TryResolvePath_Refuses_Legacy_Label_That_Escapes_Base()
    {
        // A pre-existing record whose Label tries to escape the base directory must
        // not be treated as resolvable, even if a file at that target exists.
        var record = NewRecord("../escaped");
        var outsidePath = Path.GetFullPath(Path.Combine(_tempBase, "..", "escaped.zip"));
        File.WriteAllBytes(outsidePath, new byte[] { 1 });
        try
        {
            Assert.False(BackupFileName.TryResolvePath(_tempBase, record, out var _));
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }
}
