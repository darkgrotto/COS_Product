using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step12_BackupSchedule
{
    private static readonly Dictionary<string, string> FriendlyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "daily",   "0 2 * * *" },
        { "weekly",  "0 2 * * 0" },
        { "monthly", "0 2 1 * *" }
    };

    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 12 of 17: Backup Schedule");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Configure how often backups run.");
        Console.WriteLine("Accepted values: daily, weekly, monthly, or a cron expression.");
        Console.WriteLine("Default: weekly (every Sunday at 02:00)");
        Console.WriteLine();

        config.ConfigValues.TryGetValue("backup_schedule", out var cfgSchedule);
        if (config.AutoAccept && cfgSchedule != null)
        {
            config.BackupSchedule = FriendlyNames.TryGetValue(cfgSchedule, out var autoCron) ? autoCron : cfgSchedule;
            Console.WriteLine($"Backup schedule: {config.BackupSchedule}");
        }
        else
        {
            var defaultScheduleLabel = string.IsNullOrEmpty(cfgSchedule) ? "weekly" : cfgSchedule;
            Console.Write($"Backup schedule [{defaultScheduleLabel}]: ");
            var inputRaw = Console.ReadLine()?.Trim();
            var input = string.IsNullOrEmpty(inputRaw) ? cfgSchedule : inputRaw;

            if (string.IsNullOrEmpty(input))
                config.BackupSchedule = "0 2 * * 0";
            else if (FriendlyNames.TryGetValue(input, out var cron))
                config.BackupSchedule = cron;
            else
                config.BackupSchedule = input;
        }

        Console.WriteLine($"Backup schedule: {config.BackupSchedule}");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
