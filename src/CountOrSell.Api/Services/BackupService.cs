using CountOrSell.Data;
using CountOrSell.Domain;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Api.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _db;
    private readonly ISchemaVersionService _schemaVersion;
    private readonly IBackupDestinationFactory _destinationFactory;
    private readonly IAdminNotificationService _notifications;
    private readonly IConfiguration _config;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        AppDbContext db,
        ISchemaVersionService schemaVersion,
        IBackupDestinationFactory destinationFactory,
        IAdminNotificationService notifications,
        IConfiguration config,
        IProcessRunner processRunner,
        ILogger<BackupService> logger)
    {
        _db = db;
        _schemaVersion = schemaVersion;
        _destinationFactory = destinationFactory;
        _notifications = notifications;
        _config = config;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<BackupRecord> TakeBackupAsync(BackupType backupType, CancellationToken ct)
    {
        var instanceName = _config["INSTANCE_NAME"] ?? "countorsell";
        var schemaVersion = await _schemaVersion.GetCurrentSchemaVersionAsync(ct);
        var timestamp = DateTime.UtcNow;
        var typeLabel = backupType == BackupType.Scheduled ? "scheduled" : "pre-update";
        var label = backupType == BackupType.Scheduled
            ? $"{instanceName}-scheduled-{timestamp:yyyyMMddHHmmss}"
            : $"{instanceName}-pre-update-v{schemaVersion}-{timestamp:yyyyMMddHHmmss}";

        var archiveBytes = await BuildBackupArchiveAsync(
            label, instanceName, schemaVersion, backupType, timestamp, ct);

        var record = new BackupRecord
        {
            Id = Guid.NewGuid(),
            Label = label,
            BackupType = backupType,
            SchemaVersion = schemaVersion,
            CreatedAt = timestamp,
            FileSizeBytes = archiveBytes.Length,
            IsAvailable = true
        };

        var destConfigs = await _db.BackupDestinationConfigs
            .Where(d => d.IsActive)
            .ToListAsync(ct);

        if (!destConfigs.Any())
        {
            // Fall back to local file destination
            destConfigs = new List<BackupDestinationConfig>
            {
                new()
                {
                    Id = Guid.Empty,
                    DestinationType = "local",
                    Label = "Default",
                    ConfigurationJson = "{}",
                    IsActive = true
                }
            };
        }

        foreach (var destConfig in destConfigs)
        {
            var dest = _destinationFactory.Create(destConfig);
            var destRecord = new BackupDestinationRecord
            {
                Id = Guid.NewGuid(),
                BackupRecordId = record.Id,
                DestinationType = dest.DestinationType,
                DestinationLabel = dest.Label,
                AttemptedAt = DateTime.UtcNow
            };

            try
            {
                using var stream = new MemoryStream(archiveBytes);
                await dest.WriteAsync($"{label}.zip", stream, ct);
                destRecord.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup destination {Label} failed", dest.Label);
                destRecord.Success = false;
                destRecord.ErrorMessage = ex.Message;
                await _notifications.NotifyAsync(
                    $"Backup destination '{dest.Label}' failed: {ex.Message}",
                    "backup", ct);
            }

            record.Destinations.Add(destRecord);
        }

        _db.BackupRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        await PruneOldBackupsAsync(backupType, ct);

        return record;
    }

    private async Task<byte[]> BuildBackupArchiveAsync(
        string label,
        string instanceName,
        int schemaVersion,
        BackupType backupType,
        DateTime timestamp,
        CancellationToken ct)
    {
        var connectionString =
            _config.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? "Host=localhost;Database=countorsell;Username=countorsell;Password=countorsell";

        var parsed = ParseConnectionString(connectionString);
        var tableArgs = string.Join(" ", BackupScope.Tables.Select(t => $"--table={t}"));
        var dumpSql = await RunPgDumpAsync(parsed, tableArgs, ct);

        var metadata = new BackupMetadata
        {
            BackupFormatVersion = 1,
            InstanceName = instanceName,
            SchemaVersion = schemaVersion,
            BackupType = backupType == BackupType.Scheduled ? "scheduled" : "pre-update",
            Timestamp = timestamp,
            Label = label
        };
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(
            metadata,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var metaEntry = archive.CreateEntry("metadata.json");
            await using (var metaStream = metaEntry.Open())
            {
                await metaStream.WriteAsync(
                    System.Text.Encoding.UTF8.GetBytes(metadataJson), ct);
            }

            var dumpEntry = archive.CreateEntry("dump.sql");
            await using (var dumpStream = dumpEntry.Open())
            {
                await dumpStream.WriteAsync(
                    System.Text.Encoding.UTF8.GetBytes(dumpSql), ct);
            }
        }

        return ms.ToArray();
    }

    private async Task<string> RunPgDumpAsync(
        (string Host, int Port, string Database, string Username, string Password) conn,
        string tableArgs,
        CancellationToken ct)
    {
        var args = $"-h {conn.Host} -p {conn.Port} -U {conn.Username} -d {conn.Database}" +
                   $" --no-owner --no-privileges {tableArgs}";

        var env = new Dictionary<string, string> { ["PGPASSWORD"] = conn.Password };

        return await _processRunner.RunAsync("pg_dump", args, env, null, ct);
    }

    private static (string Host, int Port, string Database, string Username, string Password)
        ParseConnectionString(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        return (
            builder.Host ?? "localhost",
            builder.Port,
            builder.Database ?? "countorsell",
            builder.Username ?? "countorsell",
            builder.Password ?? "countorsell");
    }

    private async Task PruneOldBackupsAsync(BackupType backupType, CancellationToken ct)
    {
        var retentionKey = backupType == BackupType.Scheduled
            ? "backup_retention_scheduled"
            : "backup_retention_pre_update";

        var setting = await _db.AppSettings.FindAsync(new object[] { retentionKey }, ct);
        var retention = int.TryParse(setting?.Value, out var r) ? r : 4;

        var oldRecords = await _db.BackupRecords
            .Where(b => b.BackupType == backupType)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(retention)
            .ToListAsync(ct);

        foreach (var old in oldRecords)
        {
            old.IsAvailable = false;
            var destConfigs = await _db.BackupDestinationConfigs
                .Where(d => d.IsActive)
                .ToListAsync(ct);
            foreach (var destConfig in destConfigs)
            {
                try
                {
                    var dest = _destinationFactory.Create(destConfig);
                    await dest.DeleteAsync($"{old.Label}.zip", ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to prune backup {Label} from {Dest}",
                        old.Label, destConfig.Label);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
