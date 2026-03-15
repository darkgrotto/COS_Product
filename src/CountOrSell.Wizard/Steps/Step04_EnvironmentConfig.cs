using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step04_EnvironmentConfig
{
    public static Task RunAsync(WizardConfig config)
    {
        if (config.DeploymentType == DeploymentType.Docker)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine("Step 4 of 17: Environment Configuration");
        Console.WriteLine("----------------------------------------");

        switch (config.DeploymentType)
        {
            case DeploymentType.Azure:
                ConfigureAzure(config);
                break;
            case DeploymentType.Aws:
                ConfigureAws(config);
                break;
            case DeploymentType.Gcp:
                ConfigureGcp(config);
                break;
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    private static void ConfigureAzure(WizardConfig config)
    {
        Console.WriteLine("Azure configuration:");
        Console.WriteLine();

        config.CloudSubscriptionId = PromptRequired("Azure Subscription ID");
        config.CloudResourceGroup = PromptRequired("Resource group name");
        config.CloudRegion = PromptWithDefault("Azure location", "eastus");
    }

    private static void ConfigureAws(WizardConfig config)
    {
        Console.WriteLine("AWS configuration:");
        Console.WriteLine();

        config.CloudAccessKeyId = PromptRequired("AWS Access Key ID");
        // Secret key is sensitive - store securely; prompted but not saved to config object
        Console.Write("AWS Secret Access Key: ");
        ReadPasswordLine();
        config.CloudRegion = PromptWithDefault("AWS region", "us-east-1");
    }

    private static void ConfigureGcp(WizardConfig config)
    {
        Console.WriteLine("GCP configuration:");
        Console.WriteLine();

        config.CloudProjectId = PromptRequired("GCP Project ID");
        config.CloudServiceAccountKeyPath = PromptRequired("Service account key file path");
        config.CloudRegion = PromptWithDefault("GCP region", "us-central1");
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

    private static string PromptWithDefault(string label, string defaultValue)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    private static void ReadPasswordLine()
    {
        // Read without echoing
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                sb.Append(key.KeyChar);
            }
        }
    }
}
