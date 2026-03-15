using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step05_HostingPreferences
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 5 of 17: Hosting Preferences");
        Console.WriteLine("-----------------------------------");

        while (true)
        {
            Console.Write("Hostname or subdomain (e.g. my-instance.example.com): ");
            var hostname = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(hostname))
            {
                config.Hostname = hostname;
                break;
            }
            Console.WriteLine("Hostname cannot be empty.");
        }

        if (config.DeploymentType == DeploymentType.Docker)
        {
            Console.Write("HTTPS port [443]: ");
            var portInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(portInput) && int.TryParse(portInput, out int port) && port > 0 && port <= 65535)
            {
                config.Port = port;
            }
            else
            {
                config.Port = 443;
            }
        }

        Console.WriteLine($"Hostname: {config.Hostname}");
        if (config.DeploymentType == DeploymentType.Docker)
        {
            Console.WriteLine($"Port: {config.Port}");
        }
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
