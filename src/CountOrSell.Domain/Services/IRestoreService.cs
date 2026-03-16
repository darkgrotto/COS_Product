namespace CountOrSell.Domain.Services;

public interface IRestoreService
{
    Task<RestoreResult> RestoreAsync(Stream backupStream, CancellationToken ct);
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RestoredSchemaVersion { get; set; }

    public static RestoreResult Ok(int schemaVersion) =>
        new() { Success = true, RestoredSchemaVersion = schemaVersion };

    public static RestoreResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}
