using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step13_BackupRetention
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 13 of 17: Backup Retention");
        Console.WriteLine("---------------------------------");
        Console.WriteLine("Number of backups to retain (applies separately to scheduled and pre-update backups).");
        Console.WriteLine("Default: 4");
        Console.WriteLine();

        Console.Write("Number of backups to retain [4]: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            config.BackupRetention = 4;
        }
        else if (int.TryParse(input, out int retention) && retention >= 1)
        {
            config.BackupRetention = retention;
        }
        else
        {
            Console.WriteLine("Invalid value. Using default of 4.");
            config.BackupRetention = 4;
        }

        Console.WriteLine($"Backup retention: {config.BackupRetention}");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
