using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services;

public class SchemaVersionService : ISchemaVersionService
{
    // Bump this constant when EF Core migrations are added.
    public const int ApplicationSchemaVersion = 1;

    private readonly AppDbContext _db;

    public SchemaVersionService(AppDbContext db) => _db = db;

    public async Task<int> GetCurrentSchemaVersionAsync(CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "current_schema_version" }, ct);
        return int.TryParse(setting?.Value, out var v) ? v : ApplicationSchemaVersion;
    }

    public async Task SetSchemaVersionAsync(int version, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FindAsync(new object[] { "current_schema_version" }, ct);
        if (setting != null)
        {
            setting.Value = version.ToString();
        }
        else
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = "current_schema_version",
                Value = version.ToString()
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public int GetApplicationSchemaVersion() => ApplicationSchemaVersion;
}
