using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step05_HostingPreferences
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 5 of 17: Hosting Preferences");
        Console.WriteLine("-----------------------------------");

        config.ConfigValues.TryGetValue("hostname", out var cfgHostname);
        while (true)
        {
            if (!string.IsNullOrEmpty(cfgHostname))
                Console.Write($"Hostname or subdomain (e.g. my-instance.example.com) [{cfgHostname}]: ");
            else
                Console.Write("Hostname or subdomain (e.g. my-instance.example.com): ");
            var hostnameInput = Console.ReadLine()?.Trim();
            var hostname = string.IsNullOrEmpty(hostnameInput) ? cfgHostname : hostnameInput;
            if (!string.IsNullOrEmpty(hostname))
            {
                config.Hostname = hostname;
                break;
            }
            Console.WriteLine("Hostname cannot be empty.");
        }

        if (config.DeploymentType == DeploymentType.Docker)
        {
            config.ConfigValues.TryGetValue("port", out var cfgPort);
            var defaultPort = 443;
            if (!string.IsNullOrEmpty(cfgPort) && int.TryParse(cfgPort, out int parsedCfgPort) && parsedCfgPort > 0 && parsedCfgPort <= 65535)
                defaultPort = parsedCfgPort;
            Console.Write($"HTTPS port [{defaultPort}]: ");
            var portInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(portInput) && int.TryParse(portInput, out int port) && port > 0 && port <= 65535)
                config.Port = port;
            else
                config.Port = defaultPort;
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
