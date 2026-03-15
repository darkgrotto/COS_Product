using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;

namespace CountOrSell.Wizard.Steps;

public static class Step02_Prerequisites
{
    public static Task RunAsync(WizardConfig config, PrerequisiteChecker checker)
    {
        Console.WriteLine("Step 2 of 17: Prerequisite Detection");
        Console.WriteLine("--------------------------------------");

        PrerequisiteResult result = config.DeploymentType switch
        {
            DeploymentType.Docker => checker.CheckDockerPrerequisites(),
            DeploymentType.Azure => checker.CheckAzurePrerequisites(),
            DeploymentType.Aws => checker.CheckAwsPrerequisites(),
            DeploymentType.Gcp => checker.CheckGcpPrerequisites(),
            _ => throw new InvalidOperationException("Unknown deployment type.")
        };

        if (result.AllMet)
        {
            Console.WriteLine("All prerequisites are present.");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        Console.WriteLine("The following prerequisites are missing:");
        Console.WriteLine();

        foreach (var prereq in result.Missing)
        {
            Console.WriteLine($"  MISSING: {prereq.Name}");
            Console.WriteLine($"  Install: {prereq.InstallInstructions}");
            Console.WriteLine();
        }

        while (true)
        {
            Console.Write("Have you installed all missing prerequisites? [y/N]: ");
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "Y" || input == "YES")
            {
                Console.WriteLine("Proceeding. Note: prerequisites were not re-verified.");
                Console.WriteLine();
                return Task.CompletedTask;
            }
            else
            {
                Console.WriteLine("Please install all required prerequisites before continuing.");
                Console.WriteLine("Re-checking...");
                Console.WriteLine();

                result = config.DeploymentType switch
                {
                    DeploymentType.Docker => checker.CheckDockerPrerequisites(),
                    DeploymentType.Azure => checker.CheckAzurePrerequisites(),
                    DeploymentType.Aws => checker.CheckAwsPrerequisites(),
                    DeploymentType.Gcp => checker.CheckGcpPrerequisites(),
                    _ => throw new InvalidOperationException("Unknown deployment type.")
                };

                if (result.AllMet)
                {
                    Console.WriteLine("All prerequisites are now present.");
                    Console.WriteLine();
                    return Task.CompletedTask;
                }

                Console.WriteLine("Still missing:");
                foreach (var prereq in result.Missing)
                {
                    Console.WriteLine($"  MISSING: {prereq.Name}");
                }
                Console.WriteLine();
            }
        }
    }
}
