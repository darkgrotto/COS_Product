namespace CountOrSell.Api.Services.Deployment;

// Used for Docker Compose deployments - updates are performed via update.sh, not the API.
public sealed class NullDeploymentService : ICloudDeploymentService
{
    public bool IsSupported => false;

    public Task<DeploymentResult> TriggerUpdateAsync(string? tag, CancellationToken ct)
        => Task.FromResult(DeploymentResult.NotSupported());
}
