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
        config.CloudResourceGroup = PromptRequired("Application resource group name (Terraform will create this)");
        config.CloudRegion = PromptWithDefault("Azure location", "eastus");
        Console.WriteLine();
        Console.WriteLine("Terraform state storage will be created automatically by the wizard.");
        config.CloudStateResourceGroup = PromptWithDefault("State resource group name", "countorsell-tfstate-rg");
        config.CloudStateStorageAccount = PromptStorageAccountName("Terraform state storage account name");
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
        var defaultRegion = regionCode == 0 && !string.IsNullOrWhiteSpace(detectedRegion)
            ? detectedRegion
            : "us-east-1";

        config.CloudRegion = PromptWithDefault("AWS region", defaultRegion);
        Console.WriteLine();
        Console.WriteLine("A Terraform state S3 bucket will be created automatically by the wizard.");
        Console.WriteLine("S3 bucket names are globally unique. Choose a name that is specific to your deployment.");
        config.CloudStateBucket = PromptRequired("Terraform state S3 bucket name");
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
        string defaultProject = projectCode == 0 && !string.IsNullOrWhiteSpace(detectedProject)
            ? detectedProject
            : string.Empty;

        if (!string.IsNullOrEmpty(defaultProject))
        {
            Console.WriteLine($"Detected GCP project: {defaultProject}");
            config.CloudProjectId = PromptWithDefault("GCP project ID", defaultProject);
        }
        else
        {
            config.CloudProjectId = PromptRequired("GCP project ID");
        }

        config.CloudRegion = PromptWithDefault("GCP region", "us-central1");
        Console.WriteLine();
        Console.WriteLine("A Terraform state GCS bucket will be created automatically by the wizard.");
        Console.WriteLine("GCS bucket names are globally unique. Choose a name that is specific to your deployment.");
        var defaultBucket = $"{config.CloudProjectId}-countorsell-tfstate";
        config.CloudStateBucket = PromptWithDefault("Terraform state GCS bucket name", defaultBucket);
    }

    private static string PromptRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
            Console.WriteLine($"{label} cannot be empty.");
        }
    }

    private static string PromptStorageAccountName(string label)
    {
        while (true)
        {
            Console.Write($"{label} (3-24 lowercase alphanumeric, no hyphens or special characters): ");
            var value = Console.ReadLine()?.Trim() ?? string.Empty;
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
