using CountOrSell.Domain.Models;

namespace CountOrSell.Api.Services;

// Centralizes the on-disk file-naming convention for backups so the rule lives in
// exactly one place. Filenames are derived from the immutable record GUID; the
// human-readable Label is kept for display only and never reaches the filesystem.
//
// LegacyFor() exists to read backups produced by older Product versions that wrote
// under {label}.zip. New writes always use For(); legacy reads must still go through
// the containment check in TryResolvePath to defend against malicious labels stored
// on existing records.
public static class BackupFileName
{
    public static string For(BackupRecord record) => $"{record.Id}.zip";

    public static string LegacyFor(BackupRecord record) => $"{record.Label}.zip";

    // Resolves the on-disk backup file for a record, preferring the GUID-based name
    // and falling back to LegacyFor. Every candidate is validated to live under
    // basePath, so a malicious legacy Label cannot escape the backups directory.
    public static bool TryResolvePath(string basePath, BackupRecord record, out string filePath)
    {
        var baseFull = Path.GetFullPath(basePath);
        foreach (var candidate in new[] { For(record), LegacyFor(record) })
        {
            var candidateFull = Path.GetFullPath(Path.Combine(baseFull, candidate));
            if (!candidateFull.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue; // candidate escapes the base directory - skip it
            if (File.Exists(candidateFull))
            {
                filePath = candidateFull;
                return true;
            }
        }
        filePath = string.Empty;
        return false;
    }
}
