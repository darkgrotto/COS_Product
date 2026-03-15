using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step07_Branding
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 7 of 17: Instance Branding");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("The instance name appears in the page title, header, and browser tab.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Instance name: ");
            var name = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                config.InstanceName = name;
                Console.WriteLine($"Instance name set to: {name}");
                Console.WriteLine();
                return Task.CompletedTask;
            }
            Console.WriteLine("Instance name cannot be empty.");
        }
    }
}
