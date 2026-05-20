using CountOrSell.Domain.Services;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace CountOrSell.Api.Services.Destinations;

public class GcpStorageBackupDestination : IBackupDestination
{
    private readonly StorageClient _client;
    private readonly string _bucket;
    private readonly string _projectId;

    public string DestinationType => "gcp-storage";
    public string Label { get; }

    public GcpStorageBackupDestination(
        string bucket,
        string projectId,
        string? credentialsJson,
        string? endpoint,
        string label)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException("GCS bucket name is required.", nameof(bucket));
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("GCP project ID is required.", nameof(projectId));

        _bucket = bucket;
        _projectId = projectId;
        Label = label;

        var builder = new StorageClientBuilder();
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            // Used for fake-gcs-server emulator. The Google.Apis.Storage routes are
            // declared as `b/...`, `b/{bucket}/o/...`, etc. - the production
            // `/storage/v1/` prefix is part of the default BaseUri, so any custom
            // BaseUri must include it explicitly or every request lands at
            // `/b/...` (which fake-gcs-server does not serve).
            builder.BaseUri = endpoint.TrimEnd('/') + "/storage/v1/";
            builder.UnauthenticatedAccess = true;
        }
        else if (!string.IsNullOrWhiteSpace(credentialsJson))
        {
            builder.Credential = GoogleCredential.FromJson(credentialsJson);
        }
        // else: defer to Application Default Credentials.

        _client = builder.Build();
    }

    public async Task WriteAsync(string fileName, Stream data, CancellationToken ct)
    {
        ValidateFileName(fileName);
        await EnsureBucketAsync(ct);
        await _client.UploadObjectAsync(_bucket, fileName, "application/zip", data,
            options: null, cancellationToken: ct);
    }

    public async Task<Stream> ReadAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        // Google.Cloud.Storage.V1 only exposes a download-to-stream API (no streaming
        // response handle), so to avoid buffering multi-GB backup archives in process
        // memory we stage to a temp file. DeleteOnClose ensures the file is reaped
        // when the consumer disposes the returned stream; the FileStream is seekable,
        // which ZipArchive(Read) requires.
        var tempPath = Path.GetTempFileName();
        var file = new FileStream(
            tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None,
            bufferSize: 4096, FileOptions.DeleteOnClose);
        try
        {
            await _client.DownloadObjectAsync(_bucket, fileName, file,
                options: null, cancellationToken: ct);
            file.Position = 0;
            return file;
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    public async Task<List<string>> ListFilesAsync(CancellationToken ct)
    {
        var files = new List<string>();
        if (!await BucketExistsAsync(ct))
            return files;

        await foreach (var obj in _client.ListObjectsAsync(_bucket).WithCancellation(ct))
        {
            if (obj.Name.EndsWith(".zip", StringComparison.Ordinal))
                files.Add(obj.Name);
        }
        return files;
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        try
        {
            await _client.DeleteObjectAsync(_bucket, fileName,
                options: null, cancellationToken: ct);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Idempotent - object already gone.
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct)
    {
        try
        {
            await EnsureBucketAsync(ct);
            return true;
        }
        catch (GoogleApiException)
        {
            return false;
        }
    }

    public void Dispose() => _client.Dispose();

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (await BucketExistsAsync(ct))
            return;
        await _client.CreateBucketAsync(_projectId, _bucket,
            options: null, cancellationToken: ct);
    }

    private async Task<bool> BucketExistsAsync(CancellationToken ct)
    {
        try
        {
            await _client.GetBucketAsync(_bucket, options: null, cancellationToken: ct);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // Defends against object-name path-traversal patterns and absolute paths in
    // caller-supplied names. GCS treats forward slashes as virtual prefixes, so we
    // require a plain file name matching the BackupFileName format used by BackupService.
    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            throw new ArgumentException(
                $"Object name '{fileName}' must not contain path separators or '..'.",
                nameof(fileName));
    }
}
