using Google.Cloud.Run.V2;

namespace CountOrSell.Api.Services.Deployment;

// Forces a new Cloud Run revision, which re-resolves the :latest image digest.
// Requires the service account to have roles/run.developer on the project.
// Environment variables set by Terraform: GCP_PROJECT_ID, GCP_REGION, GCP_SERVICE_NAME.
public sealed class GcpDeploymentService : ICloudDeploymentService
{
    private readonly string _projectId;
    private readonly string _region;
    private readonly string _serviceName;
    private readonly ILogger<GcpDeploymentService> _logger;

    public bool IsSupported => true;

    public GcpDeploymentService(IConfiguration configuration, ILogger<GcpDeploymentService> logger)
    {
        _projectId = configuration["GCP_PROJECT_ID"]
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID is not configured.");
        _region = configuration["GCP_REGION"]
            ?? Environment.GetEnvironmentVariable("GCP_REGION")
            ?? "us-central1";
        _serviceName = configuration["GCP_SERVICE_NAME"]
            ?? Environment.GetEnvironmentVariable("GCP_SERVICE_NAME")
            ?? throw new InvalidOperationException("GCP_SERVICE_NAME is not configured.");
        _logger = logger;
    }

    public async Task<DeploymentResult> TriggerUpdateAsync(string? tag, CancellationToken ct)
    {
        try
        {
            var client = await ServicesClient.CreateAsync(cancellationToken: ct);

            var serviceName = ServiceName.FromProjectLocationService(_projectId, _region, _serviceName);
            var service = await client.GetServiceAsync(serviceName.ToString(), ct);

            if (!string.IsNullOrWhiteSpace(tag))
            {
                // Update the container image to the new tag. Cloud Run creates a new revision
                // that pulls the specified image.
                service.Template.Containers[0].Image = $"ghcr.io/darkgrotto/countorsell:{tag}";
                _logger.LogInformation(
                    "Cloud Run image updated to tag {Tag} for {ServiceName}", tag, _serviceName);
            }
            else
            {
                // Adding or updating an annotation forces Cloud Run to create a new revision,
                // which re-resolves the :latest tag to the current image digest.
                service.Annotations["countorsell.com/deploy-triggered-at"] =
                    DateTimeOffset.UtcNow.ToString("O");
            }

            var updateRequest = new UpdateServiceRequest { Service = service };
            var operation = await client.UpdateServiceAsync(updateRequest, ct);
            await operation.PollUntilCompletedAsync(callSettings:
                Google.Api.Gax.Grpc.CallSettings.FromCancellationToken(ct));

            _logger.LogInformation("Cloud Run revision triggered for {ServiceName}", _serviceName);
            var message = string.IsNullOrWhiteSpace(tag)
                ? "Cloud Run revision triggered. The new image will be deployed."
                : $"Cloud Run updated to tag \"{tag}\" and revision triggered.";
            return DeploymentResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger Cloud Run revision for {ServiceName}", _serviceName);
            return DeploymentResult.Fail($"Failed to trigger revision: {ex.Message}");
        }
    }
}
