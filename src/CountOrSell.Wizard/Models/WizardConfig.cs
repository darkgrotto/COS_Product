namespace CountOrSell.Wizard.Models;

public class WizardConfig
{
    public DeploymentType DeploymentType { get; set; }
    public string? DockerRegistry { get; set; }
    public string DockerImageTag { get; set; } = "latest";
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public string InstanceName { get; set; } = string.Empty;
    public string DbAdminUsername { get; set; } = string.Empty;
    public string DbAdminPassword { get; set; } = string.Empty;
    public string ProductAdminUsername { get; set; } = string.Empty;
    public string ProductAdminPassword { get; set; } = string.Empty;
    public string GeneralUserUsername { get; set; } = string.Empty;
    public string GeneralUserPassword { get; set; } = string.Empty;
    public string BackupDestination { get; set; } = string.Empty;
    public string BackupConnectionString { get; set; } = string.Empty;
    public string BackupSchedule { get; set; } = "0 0 * * 0";
    public int BackupRetention { get; set; } = 4;
    public bool DownloadInitialUpdate { get; set; } = true;
    public string UpdateCheckTime { get; set; } = string.Empty;
    // Cloud-specific
    public string? CloudRegion { get; set; }
    public string? CloudSubscriptionId { get; set; }
    public string? CloudTenantId { get; set; }
    public string? CloudResourceGroup { get; set; }
    public string? CloudStateResourceGroup { get; set; }
    public string? CloudStateStorageAccount { get; set; }
    public string? CloudProjectId { get; set; }
    public string? CloudStateBucket { get; set; }
}
