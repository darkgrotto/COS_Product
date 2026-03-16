using CountOrSell.Domain.Models;
using Xunit;

namespace CountOrSell.Tests.Integration.Backup;

public class BackupMetadataVersionTest
{
    [Fact]
    public void BackupMetadata_ContainsSchemaVersion()
    {
        var metadata = new BackupMetadata
        {
            BackupFormatVersion = 1,
            InstanceName = "test",
            SchemaVersion = 42,
            BackupType = "scheduled",
            Timestamp = DateTime.UtcNow,
            Label = "test-label"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metadata);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(json);

        Assert.NotNull(parsed);
        Assert.Equal(42, parsed!.SchemaVersion);
        Assert.Equal(1, parsed.BackupFormatVersion);
        Assert.Equal("test", parsed.InstanceName);
        Assert.Equal("scheduled", parsed.BackupType);
        Assert.Equal("test-label", parsed.Label);
    }

    [Fact]
    public void BackupMetadata_PreUpdateType_RoundTrips()
    {
        var metadata = new BackupMetadata
        {
            BackupFormatVersion = 1,
            InstanceName = "myinstance",
            SchemaVersion = 7,
            BackupType = "pre-update",
            Timestamp = new DateTime(2026, 3, 16, 12, 0, 0, DateTimeKind.Utc),
            Label = "myinstance-pre-update-v7-20260316120000"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metadata);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(json);

        Assert.NotNull(parsed);
        Assert.Equal(7, parsed!.SchemaVersion);
        Assert.Equal("pre-update", parsed.BackupType);
        Assert.Equal("myinstance-pre-update-v7-20260316120000", parsed.Label);
    }

    [Fact]
    public void BackupArchive_ContainsMetadataAndDump()
    {
        var metadata = new BackupMetadata
        {
            BackupFormatVersion = 1,
            InstanceName = "test",
            SchemaVersion = 3,
            BackupType = "scheduled",
            Timestamp = DateTime.UtcNow,
            Label = "test-scheduled-20260316"
        };
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var dumpContent = "-- pg_dump output";

        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var metaEntry = archive.CreateEntry("metadata.json");
            using (var metaStream = metaEntry.Open())
            {
                metaStream.Write(System.Text.Encoding.UTF8.GetBytes(metadataJson));
            }

            var dumpEntry = archive.CreateEntry("dump.sql");
            using (var dumpStream = dumpEntry.Open())
            {
                dumpStream.Write(System.Text.Encoding.UTF8.GetBytes(dumpContent));
            }
        }

        ms.Position = 0;
        using var readArchive = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Read);

        Assert.NotNull(readArchive.GetEntry("metadata.json"));
        Assert.NotNull(readArchive.GetEntry("dump.sql"));

        var readMeta = readArchive.GetEntry("metadata.json")!;
        using var readStream = readMeta.Open();
        var readJson = new StreamReader(readStream).ReadToEnd();
        var readParsed = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(readJson);
        Assert.Equal(3, readParsed!.SchemaVersion);
    }
}
