using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;

namespace CountOrSell.Api.Services.Deployment;

// Restarts the Azure App Service, which re-pulls the Docker image.
// Requires the app's SystemAssigned managed identity to have Website Contributor on itself.
// Environment variables set by Terraform: AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP, AZURE_APP_NAME.
public sealed class AzureDeploymentService : ICloudDeploymentService
{
    private readonly string _subscriptionId;
    private readonly string _resourceGroup;
    private readonly string _appName;
    private readonly ILogger<AzureDeploymentService> _logger;

    public bool IsSupported => true;

    public AzureDeploymentService(IConfiguration configuration, ILogger<AzureDeploymentService> logger)
    {
        _subscriptionId = configuration["AZURE_SUBSCRIPTION_ID"]
            ?? Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")
            ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID is not configured.");
        _resourceGroup = configuration["AZURE_RESOURCE_GROUP"]
            ?? Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP")
            ?? throw new InvalidOperationException("AZURE_RESOURCE_GROUP is not configured.");
        _appName = configuration["AZURE_APP_NAME"]
            ?? Environment.GetEnvironmentVariable("AZURE_APP_NAME")
            ?? throw new InvalidOperationException("AZURE_APP_NAME is not configured.");
        _logger = logger;
    }

    public async Task<DeploymentResult> TriggerUpdateAsync(CancellationToken ct)
    {
        try
        {
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);

            var resourceId = WebSiteResource.CreateResourceIdentifier(
                _subscriptionId, _resourceGroup, _appName);
            var webApp = armClient.GetWebSiteResource(resourceId);

            await webApp.RestartAsync(softRestart: false, synchronous: false, cancellationToken: ct);

            _logger.LogInformation("Azure App Service restart triggered for {AppName}", _appName);
            return DeploymentResult.Ok("App Service restart triggered. The new image will be pulled on restart.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger Azure App Service restart for {AppName}", _appName);
            return DeploymentResult.Fail($"Failed to trigger restart: {ex.Message}");
        }
    }
}
