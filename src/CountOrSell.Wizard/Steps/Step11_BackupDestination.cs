using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step11_BackupDestination
{
    public static Task RunAsync(WizardConfig config)
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
                    ConfigureAzureBlob(config);
                    return Task.CompletedTask;
                case "2":
                    config.BackupDestination = "aws-s3";
                    ConfigureAwsS3(config);
                    return Task.CompletedTask;
                case "3":
                    config.BackupDestination = "gcp-storage";
                    ConfigureGcpStorage(config);
                    return Task.CompletedTask;
                case "4":
                    config.BackupDestination = "local";
                    ConfigureLocalFile(config);
                    return Task.CompletedTask;
                default:
                    Console.WriteLine("Invalid selection. Please enter 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    private static void ConfigureAzureBlob(WizardConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("Azure Blob Storage configuration:");
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
}
