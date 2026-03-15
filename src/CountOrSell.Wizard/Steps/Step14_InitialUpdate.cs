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

        Console.Write("Download and apply initial content update after deployment? [Y/n]: ");
        var input = Console.ReadLine()?.Trim().ToUpperInvariant();

        config.DownloadInitialUpdate = input != "N" && input != "NO";

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
