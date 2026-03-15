using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;

namespace CountOrSell.Wizard.Steps;

public static class Step17_UpdateCheckTime
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 17 of 17: Daily Update Check Time");
        Console.WriteLine("---------------------------------------");

        config.UpdateCheckTime = UpdateCheckTimeGenerator.Generate();

        Console.WriteLine($"Daily update checks will run at {config.UpdateCheckTime}");
        Console.WriteLine();

        // Write the update check time to config
        WriteUpdateCheckTime(config);

        return Task.CompletedTask;
    }

    private static void WriteUpdateCheckTime(WizardConfig config)
    {
        var baseDir = FindRepoRoot();

        // Update the .env file if it exists
        var envPath = Path.Combine(baseDir, ".env");
        if (File.Exists(envPath))
        {
            var lines = File.ReadAllLines(envPath).ToList();
            var found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("UPDATE_CHECK_TIME=", StringComparison.Ordinal))
                {
                    lines[i] = $"UPDATE_CHECK_TIME={config.UpdateCheckTime}";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lines.Add($"UPDATE_CHECK_TIME={config.UpdateCheckTime}");
            }

            File.WriteAllLines(envPath, lines);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }
}
