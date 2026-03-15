using System.Diagnostics;

namespace CountOrSell.Wizard.Services;

public class CommandRunner : ICommandRunner
{
    public bool CommandExists(string command)
    {
        try
        {
            var parts = command.Split(' ', 2);
            var psi = new ProcessStartInfo
            {
                FileName = parts[0],
                Arguments = parts.Length > 1 ? parts[1] + " --version" : "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
