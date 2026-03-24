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

        while (true)
        {
            Console.Write("Enter selection [1-4]: ");
            var input = Console.ReadLine()?.Trim();

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
        Console.Write("Storage account name: ");
        var accountName = Console.ReadLine()?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(accountName))
        {
            Console.WriteLine("Retrieving connection string from Azure...");
            var (exitCode, connectionString) = await RunAndCaptureAsync("az",
                $"storage account show-connection-string --name {accountName} --output tsv --query connectionString");

            if (exitCode == 0 && !string.IsNullOrWhiteSpace(connectionString))
            {
                config.BackupConnectionString = connectionString;
                Console.WriteLine("Connection string retrieved automatically.");
                Console.WriteLine("Azure Blob Storage backup destination configured.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Could not retrieve connection string automatically.");
            Console.WriteLine("This may be because you are not logged in to Azure CLI, or the account");
            Console.WriteLine("is in a different subscription. Enter the connection string manually.");
            Console.WriteLine();
            Console.WriteLine("To find it in the Azure portal:");
            Console.WriteLine("  1. Open your storage account.");
            Console.WriteLine("  2. Go to Security + networking > Access keys.");
            Console.WriteLine("  3. Copy the Connection string for key1 or key2.");
            Console.WriteLine();
        }

        Console.Write("Connection string: ");
        config.BackupConnectionString = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.WriteLine("Azure Blob Storage backup destination configured.");
        Console.WriteLine();
    }

    private static void ConfigureAwsS3(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("AWS S3 configuration:");
        Console.Write("S3 bucket name: ");
        var bucket = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("AWS region: ");
        var region = Console.ReadLine()?.Trim() ?? "us-east-1";
        config.BackupConnectionString = $"s3://{bucket}?region={region}";
        Console.WriteLine("AWS S3 backup destination configured.");
        Console.WriteLine();
    }

    private static void ConfigureGcpStorage(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("GCP Storage configuration:");
        Console.Write("GCS bucket name: ");
        var bucket = Console.ReadLine()?.Trim() ?? string.Empty;
        config.BackupConnectionString = $"gs://{bucket}";
        Console.WriteLine("GCP Storage backup destination configured.");
        Console.WriteLine();
    }

    private static void ConfigureLocalFile(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("Local file export configuration:");
        Console.Write("Local backup directory path [./backups]: ");
        var path = Console.ReadLine()?.Trim();
        config.BackupConnectionString = string.IsNullOrEmpty(path) ? "./backups" : path;
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
