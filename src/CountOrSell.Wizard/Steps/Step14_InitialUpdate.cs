using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step14_InitialUpdate
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 14 of 17: Initial Content Update");
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("CountOrSell can download and apply the initial card/set content update");
        Console.WriteLine("from countorsell.com after deployment.");
        Console.WriteLine();

        config.ConfigValues.TryGetValue("initial_update", out var cfgInitialUpdate);
        var defaultUpdate = cfgInitialUpdate?.ToUpperInvariant() != "N" && cfgInitialUpdate?.ToUpperInvariant() != "NO" && cfgInitialUpdate?.ToUpperInvariant() != "FALSE";
        Console.Write($"Download and apply initial content update after deployment? [{(defaultUpdate ? "Y/n" : "y/N")}]: ");
        var inputRaw = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(inputRaw))
            config.DownloadInitialUpdate = defaultUpdate;
        else
            config.DownloadInitialUpdate = inputRaw != "N" && inputRaw != "NO";

        if (config.DownloadInitialUpdate)
        {
            Console.WriteLine("Initial content update will be downloaded after deployment.");
        }
        else
        {
            Console.WriteLine("Skipping initial content update. You can trigger it manually from the admin panel.");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }
}
