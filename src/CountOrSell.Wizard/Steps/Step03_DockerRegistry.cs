using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step03_DockerRegistry
{
    public static Task RunAsync(WizardConfig config)
    {
        if (config.DeploymentType != DeploymentType.Docker)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine("Step 3 of 17: Docker Image Registry");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Specify the Docker image registry where CountOrSell images are hosted.");
        Console.WriteLine("Example: ghcr.io/yourorg/countorsell");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Docker image registry: ");
            var input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input))
            {
                config.DockerRegistry = input;
                Console.WriteLine($"Registry set to: {input}");
                Console.WriteLine();
                return Task.CompletedTask;
            }

            Console.WriteLine("Registry cannot be empty. Please enter a valid registry.");
        }
    }
}
