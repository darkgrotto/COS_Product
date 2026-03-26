using Amazon.AppRunner;
using Amazon.AppRunner.Model;

namespace CountOrSell.Api.Services.Deployment;

// Triggers a new App Runner deployment, which re-pulls the Docker image.
// Requires the instance role to have apprunner:StartDeployment and apprunner:ListServices.
// Environment variables set by Terraform: CLOUD_APP_RUNNER_SERVICE_NAME, CLOUD_REGION.
public sealed class AwsDeploymentService : ICloudDeploymentService
{
    private readonly string _serviceName;
    private readonly string _region;
    private readonly ILogger<AwsDeploymentService> _logger;

    public bool IsSupported => true;

    public AwsDeploymentService(IConfiguration configuration, ILogger<AwsDeploymentService> logger)
    {
        _serviceName = configuration["CLOUD_APP_RUNNER_SERVICE_NAME"]
            ?? Environment.GetEnvironmentVariable("CLOUD_APP_RUNNER_SERVICE_NAME")
            ?? throw new InvalidOperationException("CLOUD_APP_RUNNER_SERVICE_NAME is not configured.");
        _region = configuration["CLOUD_REGION"]
            ?? Environment.GetEnvironmentVariable("CLOUD_REGION")
            ?? "us-east-1";
        _logger = logger;
    }

    public async Task<DeploymentResult> TriggerUpdateAsync(CancellationToken ct)
    {
        try
        {
            var client = new AmazonAppRunnerClient(Amazon.RegionEndpoint.GetBySystemName(_region));

            // Resolve service name to ARN
            string? serviceArn = null;
            string? nextToken = null;
            do
            {
                var listRequest = new ListServicesRequest { NextToken = nextToken };
                var listResponse = await client.ListServicesAsync(listRequest, ct);
                var match = listResponse.ServiceSummaryList
                    .FirstOrDefault(s => s.ServiceName == _serviceName);
                if (match != null)
                {
                    serviceArn = match.ServiceArn;
                    break;
                }
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            if (serviceArn == null)
            {
                _logger.LogError("App Runner service {ServiceName} not found in region {Region}",
                    _serviceName, _region);
                return DeploymentResult.Fail($"App Runner service '{_serviceName}' not found.");
            }

            var deployRequest = new StartDeploymentRequest { ServiceArn = serviceArn };
            await client.StartDeploymentAsync(deployRequest, ct);

            _logger.LogInformation("App Runner deployment triggered for {ServiceName}", _serviceName);
            return DeploymentResult.Ok("App Runner deployment triggered. The new image will be pulled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger App Runner deployment for {ServiceName}", _serviceName);
            return DeploymentResult.Fail($"Failed to trigger deployment: {ex.Message}");
        }
    }
}
