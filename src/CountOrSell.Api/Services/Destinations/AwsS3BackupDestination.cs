using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CountOrSell.Domain.Services;

namespace CountOrSell.Api.Services.Destinations;

public class AwsS3BackupDestination : IBackupDestination
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public string DestinationType => "aws-s3";
    public string Label { get; }

    public AwsS3BackupDestination(
        string bucket,
        string region,
        string? accessKey,
        string? secretKey,
        string? serviceUrl,
        string label)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException("S3 bucket name is required.", nameof(bucket));
        if (string.IsNullOrWhiteSpace(region))
            throw new ArgumentException("AWS region is required.", nameof(region));

        _bucket = bucket;
        Label = label;

        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            // Used for LocalStack / MinIO. Path-style addressing avoids requiring
            // bucket-name DNS resolution against the override endpoint.
            s3Config.ServiceURL = serviceUrl;
            s3Config.ForcePathStyle = true;
        }

        // Both checks intentionally mirror each other so a partial-paste of one half
        // of a credential pair fails loudly at config time rather than silently
        // falling through to the SDK default credential chain (and then breaking
        // mysteriously months later when the ambient credentials rotate).
        if (!string.IsNullOrWhiteSpace(accessKey) && string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException(
                "S3 secretKey is required when accessKey is provided.", nameof(secretKey));
        if (!string.IsNullOrWhiteSpace(secretKey) && string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException(
                "S3 accessKey is required when secretKey is provided.", nameof(accessKey));

        if (!string.IsNullOrWhiteSpace(accessKey))
        {
            var creds = new BasicAWSCredentials(accessKey, secretKey);
            _s3 = new AmazonS3Client(creds, s3Config);
        }
        else
        {
            // Default credential provider chain: env vars, shared credentials file,
            // EC2/ECS instance metadata, etc.
            _s3 = new AmazonS3Client(s3Config);
        }
    }

    public async Task WriteAsync(string fileName, Stream data, CancellationToken ct)
    {
        ValidateFileName(fileName);
        await EnsureBucketAsync(ct);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = fileName,
            InputStream = data,
            AutoCloseStream = false
        };
        await _s3.PutObjectAsync(request, ct);
    }

    public async Task<Stream> ReadAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        var response = await _s3.GetObjectAsync(_bucket, fileName, ct);
        return response.ResponseStream;
    }

    public async Task<List<string>> ListFilesAsync(CancellationToken ct)
    {
        var files = new List<string>();
        if (!await BucketExistsAsync(ct))
            return files;

        string? continuationToken = null;
        do
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                ContinuationToken = continuationToken
            }, ct);

            foreach (var obj in response.S3Objects)
            {
                if (obj.Key.EndsWith(".zip", StringComparison.Ordinal))
                    files.Add(obj.Key);
            }

            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        }
        while (continuationToken != null);

        return files;
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        ValidateFileName(fileName);
        // S3 DeleteObject returns 204 whether or not the key existed, so this is naturally
        // idempotent and needs no pre-check.
        await _s3.DeleteObjectAsync(_bucket, fileName, ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct)
    {
        try
        {
            await EnsureBucketAsync(ct);
            return true;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    public void Dispose() => _s3.Dispose();

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (await BucketExistsAsync(ct))
            return;
        await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, ct);
    }

    private async Task<bool> BucketExistsAsync(CancellationToken ct)
    {
        // GetBucketLocation requires s3:GetBucketLocation which is rarely granted in
        // least-privilege deployments where the bucket is provisioned by Terraform/CDK
        // and the app's IAM policy is scoped to object actions plus s3:ListBucket. We
        // use a 1-key ListObjectsV2 instead - it is authorized by s3:ListBucket (already
        // required by ListFilesAsync) and returns 404 NoSuchBucket distinguishably.
        try
        {
            await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                MaxKeys = 1
            }, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound
                                            || ex.ErrorCode == "NoSuchBucket")
        {
            return false;
        }
    }

    // Defends against object-key path-traversal patterns and absolute paths in
    // caller-supplied names. S3 treats forward slashes as virtual prefixes, so we
    // require a plain file name matching the BackupFileName format used by BackupService.
    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            throw new ArgumentException(
                $"Object key '{fileName}' must not contain path separators or '..'.",
                nameof(fileName));
    }
}
