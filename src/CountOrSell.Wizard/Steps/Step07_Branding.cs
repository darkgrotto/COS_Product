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

        config.ConfigValues.TryGetValue("instance_name", out var cfgInstanceName);
        if (config.AutoAccept && cfgInstanceName != null)
        {
            config.InstanceName = cfgInstanceName;
            Console.WriteLine($"Instance name: {cfgInstanceName}");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        while (true)
        {
            if (!string.IsNullOrEmpty(cfgInstanceName))
                Console.Write($"Instance name [{cfgInstanceName}]: ");
            else
                Console.Write("Instance name: ");
            var nameInput = Console.ReadLine()?.Trim();
            var name = string.IsNullOrEmpty(nameInput) ? cfgInstanceName : nameInput;
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
