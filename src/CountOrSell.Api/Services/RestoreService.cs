using CountOrSell.Domain;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class RestoreService : IRestoreService
{
    private readonly ISchemaVersionService _schemaVersion;
    private readonly IConfiguration _config;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<RestoreService> _logger;

    public RestoreService(
        ISchemaVersionService schemaVersion,
        IConfiguration config,
        IProcessRunner processRunner,
        ILogger<RestoreService> logger)
    {
        _schemaVersion = schemaVersion;
        _config = config;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<RestoreResult> RestoreAsync(Stream backupStream, CancellationToken ct)
    {
        using var archive = new System.IO.Compression.ZipArchive(
            backupStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);

        var metaEntry = archive.GetEntry("metadata.json")
            ?? throw new InvalidOperationException("Backup archive missing metadata.json");

        using var metaStream = metaEntry.Open();
        var metadataJson = await new StreamReader(metaStream).ReadToEndAsync(ct);
        var metadata = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(metadataJson)
            ?? throw new InvalidOperationException("Failed to parse backup metadata");

        var currentSchema = _schemaVersion.GetApplicationSchemaVersion();
        if (metadata.SchemaVersion > currentSchema)
        {
            return RestoreResult.Fail(
                $"Cannot restore: backup schema version {metadata.SchemaVersion} is newer than " +
                $"current deployment schema version {currentSchema}. " +
                $"Update the deployment to schema version {metadata.SchemaVersion} or higher before restoring.");
        }

        var dumpEntry = archive.GetEntry("dump.sql")
            ?? throw new InvalidOperationException("Backup archive missing dump.sql");

        using var dumpStream = dumpEntry.Open();
        var dumpSql = await new StreamReader(dumpStream).ReadToEndAsync(ct);

        await RunRestoreAsync(dumpSql, ct);

        _logger.LogInformation(
            "Restore completed from backup with schema version {Version}",
            metadata.SchemaVersion);

        return RestoreResult.Ok(metadata.SchemaVersion);
    }

    private async Task RunRestoreAsync(string dumpSql, CancellationToken ct)
    {
        var connectionString =
            _config.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException(
                "Database connection string is not configured. Set POSTGRES_CONNECTION " +
                "(env var) or ConnectionStrings:Default (configuration).");

        var conn = ParseConnectionString(connectionString);

        var dropSql = string.Join("\n",
            BackupScope.Tables.Select(t => $"DROP TABLE IF EXISTS \"{t}\" CASCADE;"));

        var args = $"-h {conn.Host} -p {conn.Port} -U {conn.Username} -d {conn.Database}";
        var env = new Dictionary<string, string> { ["PGPASSWORD"] = conn.Password };
        var input = dropSql + "\n" + dumpSql;

        await _processRunner.RunAsync("psql", args, env, input, ct);
    }

    private static (string Host, int Port, string Database, string Username, string Password)
        ParseConnectionString(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host)
            || string.IsNullOrWhiteSpace(builder.Database)
            || string.IsNullOrWhiteSpace(builder.Username)
            || string.IsNullOrWhiteSpace(builder.Password))
            throw new InvalidOperationException(
                "Database connection string is missing one or more required fields " +
                "(Host, Database, Username, Password).");
        return (builder.Host, builder.Port, builder.Database, builder.Username, builder.Password);
    }
}
