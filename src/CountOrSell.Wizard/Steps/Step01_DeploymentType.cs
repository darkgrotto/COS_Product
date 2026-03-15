using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step01_DeploymentType
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 1 of 17: Deployment Type");
        Console.WriteLine("------------------------------");
        Console.WriteLine("Select your deployment target:");
        Console.WriteLine("  1) Azure (App Service)");
        Console.WriteLine("  2) AWS (App Runner)");
        Console.WriteLine("  3) GCP (Cloud Run)");
        Console.WriteLine("  4) Docker Compose");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Enter selection [1-4]: ");
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    config.DeploymentType = DeploymentType.Azure;
                    Console.WriteLine("Selected: Azure");
                    Console.WriteLine();
                    return Task.CompletedTask;
                case "2":
                    config.DeploymentType = DeploymentType.Aws;
                    Console.WriteLine("Selected: AWS");
                    Console.WriteLine();
                    return Task.CompletedTask;
                case "3":
                    config.DeploymentType = DeploymentType.Gcp;
                    Console.WriteLine("Selected: GCP");
                    Console.WriteLine();
                    return Task.CompletedTask;
                case "4":
                    config.DeploymentType = DeploymentType.Docker;
                    Console.WriteLine("Selected: Docker Compose");
                    Console.WriteLine();
                    return Task.CompletedTask;
                default:
                    Console.WriteLine("Invalid selection. Please enter 1, 2, 3, or 4.");
                    break;
            }
        }
    }
}
