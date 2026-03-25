using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
using System.Text.Json;

namespace CountOrSell.Wizard.Steps;

public static class Step04_EnvironmentConfig
{
    public static async Task RunAsync(WizardConfig config, ICommandRunner runner)
    {
        if (config.DeploymentType == DeploymentType.Docker)
        {
            return;
        }

        Console.WriteLine("Step 4 of 17: Environment Configuration");
        Console.WriteLine("----------------------------------------");

        switch (config.DeploymentType)
        {
            case DeploymentType.Azure:
                await ConfigureAzureAsync(config, runner);
                break;
            case DeploymentType.Aws:
                await ConfigureAwsAsync(config, runner);
                break;
            case DeploymentType.Gcp:
                await ConfigureGcpAsync(config, runner);
                break;
        }

        Console.WriteLine();
    }

    private static async Task ConfigureAzureAsync(WizardConfig config, ICommandRunner runner)
    {
        Console.WriteLine("Checking Azure login...");

        while (true)
        {
            var (exitCode, output) = await runner.RunWithOutputAsync("az", "account show --output json");
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    var doc = JsonDocument.Parse(output);
                    config.CloudSubscriptionId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
                    config.CloudTenantId = doc.RootElement.GetProperty("tenantId").GetString() ?? string.Empty;
                    Console.WriteLine($"Logged in. Subscription: {config.CloudSubscriptionId}");
                    Console.WriteLine($"Tenant: {config.CloudTenantId}");
                    break;
                }
                catch
                {
                    // fall through to login prompt
                }
            }

            Console.WriteLine("Not logged in to Azure.");
            Console.WriteLine("Run the following command in another terminal, then press Enter:");
            Console.WriteLine("  az login");
            Console.Write("Press Enter when logged in: ");
            Console.ReadLine();
        }

        Console.WriteLine();
        config.ConfigValues.TryGetValue("application_resource_group", out var cfgAppRg);
        config.CloudResourceGroup = PromptRequired("Application resource group name (Terraform will create this)", cfgAppRg ?? "");
        config.ConfigValues.TryGetValue("location", out var cfgLocation);
        config.CloudRegion = PromptWithDefault("Azure location", cfgLocation ?? "eastus");
        Console.WriteLine();
        Console.WriteLine("Terraform state storage will be created automatically by the wizard.");
        config.ConfigValues.TryGetValue("state_resource_group", out var cfgStateRg);
        config.CloudStateResourceGroup = PromptWithDefault("State resource group name", cfgStateRg ?? "countorsell-tfstate-rg");
        config.ConfigValues.TryGetValue("state_storage_account", out var cfgStateAccount);
        config.CloudStateStorageAccount = PromptStorageAccountName("Terraform state storage account name", cfgStateAccount ?? "");
    }

    private static async Task ConfigureAwsAsync(WizardConfig config, ICommandRunner runner)
    {
        Console.WriteLine("Checking AWS credentials...");

        while (true)
        {
            var (exitCode, _) = await runner.RunWithOutputAsync("aws", "sts get-caller-identity --output json");
            if (exitCode == 0)
            {
                Console.WriteLine("AWS credentials valid.");
                break;
            }

            Console.WriteLine("AWS credentials are not configured or are invalid.");
            Console.WriteLine("Configure credentials in another terminal using one of:");
            Console.WriteLine("  aws configure");
            Console.WriteLine("  export AWS_ACCESS_KEY_ID=... && export AWS_SECRET_ACCESS_KEY=...");
            Console.Write("Press Enter when credentials are ready: ");
            Console.ReadLine();
        }

        Console.WriteLine();

        var (regionCode, detectedRegion) = await runner.RunWithOutputAsync("aws", "configure get region");
        config.ConfigValues.TryGetValue("region", out var cfgRegion);
        var defaultRegion = cfgRegion
            ?? (regionCode == 0 && !string.IsNullOrWhiteSpace(detectedRegion) ? detectedRegion : null)
            ?? "us-east-1";

        config.CloudRegion = PromptWithDefault("AWS region", defaultRegion);
        Console.WriteLine();
        Console.WriteLine("A Terraform state S3 bucket will be created automatically by the wizard.");
        Console.WriteLine("S3 bucket names are globally unique. Choose a name that is specific to your deployment.");
        config.ConfigValues.TryGetValue("state_bucket", out var cfgStateBucket);
        config.CloudStateBucket = PromptRequired("Terraform state S3 bucket name", cfgStateBucket ?? "");
    }

    private static async Task ConfigureGcpAsync(WizardConfig config, ICommandRunner runner)
    {
        Console.WriteLine("Checking gcloud user login...");

        while (true)
        {
            var (exitCode, _) = await runner.RunWithOutputAsync("gcloud", "auth print-access-token");
            if (exitCode == 0)
            {
                Console.WriteLine("gcloud user login confirmed.");
                break;
            }

            Console.WriteLine("Not logged in to gcloud.");
            Console.WriteLine("Run the following command in another terminal, then press Enter:");
            Console.WriteLine("  gcloud auth login");
            Console.Write("Press Enter when logged in: ");
            Console.ReadLine();
        }

        Console.WriteLine("Checking application default credentials...");

        while (true)
        {
            var (exitCode, _) = await runner.RunWithOutputAsync("gcloud", "auth application-default print-access-token");
            if (exitCode == 0)
            {
                Console.WriteLine("Application default credentials confirmed.");
                break;
            }

            Console.WriteLine("Application default credentials are not configured.");
            Console.WriteLine("Run the following command in another terminal, then press Enter:");
            Console.WriteLine("  gcloud auth application-default login");
            Console.Write("Press Enter when complete: ");
            Console.ReadLine();
        }

        Console.WriteLine();

        var (projectCode, detectedProject) = await runner.RunWithOutputAsync("gcloud", "config get-value project");
        config.ConfigValues.TryGetValue("project_id", out var cfgProject);
        var defaultProject = cfgProject
            ?? (projectCode == 0 && !string.IsNullOrWhiteSpace(detectedProject) ? detectedProject : null)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(defaultProject))
        {
            if (string.IsNullOrEmpty(cfgProject))
                Console.WriteLine($"Detected GCP project: {defaultProject}");
            config.CloudProjectId = PromptWithDefault("GCP project ID", defaultProject);
        }
        else
        {
            config.CloudProjectId = PromptRequired("GCP project ID");
        }

        config.ConfigValues.TryGetValue("region", out var cfgGcpRegion);
        config.CloudRegion = PromptWithDefault("GCP region", cfgGcpRegion ?? "us-central1");
        Console.WriteLine();
        Console.WriteLine("A Terraform state GCS bucket will be created automatically by the wizard.");
        Console.WriteLine("GCS bucket names are globally unique. Choose a name that is specific to your deployment.");
        config.ConfigValues.TryGetValue("state_bucket", out var cfgGcpBucket);
        var defaultBucket = cfgGcpBucket ?? $"{config.CloudProjectId}-countorsell-tfstate";
        config.CloudStateBucket = PromptWithDefault("Terraform state GCS bucket name", defaultBucket);
    }

    private static string PromptRequired(string label, string defaultValue = "")
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(defaultValue))
                Console.Write($"{label} [{defaultValue}]: ");
            else
                Console.Write($"{label}: ");
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(value))
                return value;
            if (!string.IsNullOrEmpty(defaultValue))
                return defaultValue;
            Console.WriteLine($"{label} cannot be empty.");
        }
    }

    private static string PromptStorageAccountName(string label, string defaultValue = "")
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(defaultValue))
                Console.Write($"{label} (3-24 lowercase alphanumeric, no hyphens or special characters) [{defaultValue}]: ");
            else
                Console.Write($"{label} (3-24 lowercase alphanumeric, no hyphens or special characters): ");
            var raw = Console.ReadLine()?.Trim() ?? string.Empty;
            var value = string.IsNullOrEmpty(raw) ? defaultValue : raw;
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("Storage account name cannot be empty.");
                continue;
            }
            if (value.Length < 3 || value.Length > 24)
            {
                Console.WriteLine($"Storage account name must be 3-24 characters (got {value.Length}).");
                continue;
            }
            if (!value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c)))
            {
                Console.WriteLine("Storage account name may only contain lowercase letters and digits.");
                continue;
            }
            return value;
        }
    }

    private static string PromptWithDefault(string label, string defaultValue)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
}
