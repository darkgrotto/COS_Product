namespace CountOrSell.Api.Services.Deployment;

public interface ICloudDeploymentService
{
    bool IsSupported { get; }
    // tag: specific image tag to deploy (e.g. "dev", "1.2.3"). Null means re-deploy the
    // currently configured tag without changing it.
    Task<DeploymentResult> TriggerUpdateAsync(string? tag, CancellationToken ct);
}

public sealed class DeploymentResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static DeploymentResult Ok(string message) => new() { Success = true, Message = message };
    public static DeploymentResult Fail(string message) => new() { Success = false, Message = message };
    public static DeploymentResult NotSupported() => new()
    {
        Success = false,
        Message = "Application updates for this deployment type are managed via the generated update.sh script."
    };
}
