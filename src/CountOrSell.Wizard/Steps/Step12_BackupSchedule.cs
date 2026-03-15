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

        Console.Write("Backup schedule [weekly]: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            config.BackupSchedule = "0 2 * * 0";
        }
        else if (FriendlyNames.TryGetValue(input, out var cron))
        {
            config.BackupSchedule = cron;
        }
        else
        {
            // Accept as raw cron expression
            config.BackupSchedule = input;
        }

        Console.WriteLine($"Backup schedule: {config.BackupSchedule}");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
