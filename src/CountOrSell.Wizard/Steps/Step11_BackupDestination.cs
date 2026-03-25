using CountOrSell.Wizard.Models;
using System.Diagnostics;

namespace CountOrSell.Wizard.Steps;

public static class Step11_BackupDestination
{
    public static async Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 11 of 17: Backup Destination");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine("Select the primary backup destination.");
        Console.WriteLine("Additional destinations can be configured after setup.");
        Console.WriteLine();
        Console.WriteLine("  1) Azure Blob Storage");
        Console.WriteLine("  2) AWS S3");
        Console.WriteLine("  3) GCP Storage");
        Console.WriteLine("  4) Local file export");
        Console.WriteLine();

        config.ConfigValues.TryGetValue("backup_destination", out var cfgBackupDest);
        var defaultSelection = cfgBackupDest switch
        {
            "azure-blob"  => "1",
            "aws-s3"      => "2",
            "gcp-storage" => "3",
            "local"       => "4",
            _             => null
        };

        while (true)
        {
            if (defaultSelection != null)
                Console.Write($"Enter selection [1-4] [{defaultSelection}]: ");
            else
                Console.Write("Enter selection [1-4]: ");
            var raw = Console.ReadLine()?.Trim();
            var input = string.IsNullOrEmpty(raw) ? defaultSelection : raw;

            switch (input)
            {
                case "1":
                    config.BackupDestination = "azure-blob";
                    await ConfigureAzureBlobAsync(config);
                    return;
                case "2":
                    config.BackupDestination = "aws-s3";
                    ConfigureAwsS3(config);
                    return;
                case "3":
                    config.BackupDestination = "gcp-storage";
                    ConfigureGcpStorage(config);
                    return;
                case "4":
                    config.BackupDestination = "local";
                    ConfigureLocalFile(config);
                    return;
                default:
                    Console.WriteLine("Invalid selection. Please enter 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    private static async Task ConfigureAzureBlobAsync(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("Azure Blob Storage configuration:");
        Console.WriteLine("The wizard will create the storage account and retrieve the connection string.");
        Console.WriteLine();

        config.ConfigValues.TryGetValue("backup_azure_account", out var cfgAzureAccount);
        var accountName = PromptStorageAccountName(cfgAzureAccount ?? "");

        config.ConfigValues.TryGetValue("backup_azure_resource_group", out var cfgAzureRg);
        var defaultRg = cfgAzureRg ?? "countorsell-backup-rg";
        Console.Write($"Resource group [{defaultRg}]: ");
        var rgInput = Console.ReadLine()?.Trim();
        var resourceGroup = string.IsNullOrEmpty(rgInput) ? defaultRg : rgInput;

        var defaultLocation = !string.IsNullOrEmpty(config.CloudRegion) ? config.CloudRegion : "eastus";
        Console.Write($"Azure location [{defaultLocation}]: ");
        var locationInput = Console.ReadLine()?.Trim();
        var location = string.IsNullOrEmpty(locationInput) ? defaultLocation : locationInput;

        if (!string.IsNullOrEmpty(accountName))
        {
            Console.WriteLine();
            Console.WriteLine("Provisioning Azure Blob Storage...");

            int rgCode = await RunCommandAsync("az",
                $"group create --name {resourceGroup} --location {location} --output none");
            if (rgCode != 0)
            {
                Console.WriteLine($"WARNING: az group create exited with code {rgCode}. Continuing.");
            }

            int accountCode = await RunCommandAsync("az",
                $"storage account create --name {accountName} --resource-group {resourceGroup} " +
                $"--location {location} --sku Standard_LRS --allow-blob-public-access false --output none");
            if (accountCode != 0)
            {
                Console.WriteLine($"WARNING: az storage account create exited with code {accountCode}.");
                Console.WriteLine("Falling back to manual connection string entry.");
                Console.WriteLine();
                goto manualEntry;
            }

            var (connCode, connectionString) = await RunAndCaptureAsync("az",
                $"storage account show-connection-string --name {accountName} " +
                $"--resource-group {resourceGroup} --output tsv --query connectionString");

            if (connCode == 0 && !string.IsNullOrWhiteSpace(connectionString))
            {
                config.BackupConnectionString = connectionString;
                Console.WriteLine("Storage account created and connection string retrieved.");
                Console.WriteLine("Azure Blob Storage backup destination configured.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Storage account created but connection string could not be retrieved.");
            Console.WriteLine("Falling back to manual connection string entry.");
            Console.WriteLine();
        }

        manualEntry:
        Console.WriteLine("To find the connection string in the Azure portal:");
        Console.WriteLine("  1. Open your storage account.");
        Console.WriteLine("  2. Go to Security + networking > Access keys.");
        Console.WriteLine("  3. Copy the Connection string for key1 or key2.");
        Console.WriteLine();
        Console.Write("Connection string: ");
        config.BackupConnectionString = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.WriteLine("Azure Blob Storage backup destination configured.");
        Console.WriteLine();
    }

    private static string PromptStorageAccountName(string defaultValue = "")
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(defaultValue))
                Console.Write($"Storage account name (3-24 lowercase alphanumeric, no hyphens or special characters) [{defaultValue}]: ");
            else
                Console.Write("Storage account name (3-24 lowercase alphanumeric, no hyphens or special characters): ");
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

    private static async Task<int> RunCommandAsync(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };

            using var proc = Process.Start(psi);
            if (proc == null) return -1;
            await proc.WaitForExitAsync();
            return proc.ExitCode;
        }
        catch
        {
            return -1;
        }
    }

    private static void ConfigureAwsS3(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("AWS S3 configuration:");

        config.ConfigValues.TryGetValue("backup_aws_bucket", out var cfgAwsBucket);
        if (!string.IsNullOrEmpty(cfgAwsBucket))
            Console.Write($"S3 bucket name [{cfgAwsBucket}]: ");
        else
            Console.Write("S3 bucket name: ");
        var bucketInput = Console.ReadLine()?.Trim();
        var bucket = string.IsNullOrEmpty(bucketInput) ? (cfgAwsBucket ?? string.Empty) : bucketInput;

        config.ConfigValues.TryGetValue("backup_aws_region", out var cfgAwsRegion);
        var defaultRegion = cfgAwsRegion ?? (config.CloudRegion ?? "us-east-1");
        Console.Write($"AWS region [{defaultRegion}]: ");
        var regionInput = Console.ReadLine()?.Trim();
        var region = string.IsNullOrEmpty(regionInput) ? defaultRegion : regionInput;

        config.BackupConnectionString = $"s3://{bucket}?region={region}";
        Console.WriteLine("AWS S3 backup destination configured.");
        Console.WriteLine();
    }

    private static void ConfigureGcpStorage(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("GCP Storage configuration:");

        config.ConfigValues.TryGetValue("backup_gcp_bucket", out var cfgGcpBucket);
        if (!string.IsNullOrEmpty(cfgGcpBucket))
            Console.Write($"GCS bucket name [{cfgGcpBucket}]: ");
        else
            Console.Write("GCS bucket name: ");
        var bucketInput = Console.ReadLine()?.Trim();
        var bucket = string.IsNullOrEmpty(bucketInput) ? (cfgGcpBucket ?? string.Empty) : bucketInput;

        config.BackupConnectionString = $"gs://{bucket}";
        Console.WriteLine("GCP Storage backup destination configured.");
        Console.WriteLine();
    }

    private static void ConfigureLocalFile(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("Local file export configuration:");

        config.ConfigValues.TryGetValue("backup_local_path", out var cfgLocalPath);
        var defaultPath = cfgLocalPath ?? "./backups";
        Console.Write($"Local backup directory path [{defaultPath}]: ");
        var pathInput = Console.ReadLine()?.Trim();
        config.BackupConnectionString = string.IsNullOrEmpty(pathInput) ? defaultPath : pathInput;
        Console.WriteLine($"Local backup path: {config.BackupConnectionString}");
        Console.WriteLine();
    }

    private static async Task<(int ExitCode, string Output)> RunAndCaptureAsync(
        string command,
        string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return (-1, string.Empty);
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return (proc.ExitCode, output.Trim());
        }
        catch
        {
            return (-1, string.Empty);
        }
    }
}
