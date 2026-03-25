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

        config.ConfigValues.TryGetValue("backup_retention", out var cfgRetention);
        var defaultRetention = 4;
        if (!string.IsNullOrEmpty(cfgRetention) && int.TryParse(cfgRetention, out int parsedCfgRetention) && parsedCfgRetention >= 1)
            defaultRetention = parsedCfgRetention;

        if (config.AutoAccept && cfgRetention != null)
        {
            config.BackupRetention = defaultRetention;
            Console.WriteLine($"Backup retention: {config.BackupRetention}");
        }
        else
        {
            Console.Write($"Number of backups to retain [{defaultRetention}]: ");
            var inputRaw = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(inputRaw))
                config.BackupRetention = defaultRetention;
            else if (int.TryParse(inputRaw, out int retention) && retention >= 1)
                config.BackupRetention = retention;
            else
            {
                Console.WriteLine($"Invalid value. Using default of {defaultRetention}.");
                config.BackupRetention = defaultRetention;
            }
        }

        Console.WriteLine($"Backup retention: {config.BackupRetention}");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
