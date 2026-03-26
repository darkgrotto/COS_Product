namespace CountOrSell.Api.Services.Deployment;

public interface ICloudDeploymentService
{
    bool IsSupported { get; }
    Task<DeploymentResult> TriggerUpdateAsync(CancellationToken ct);
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
