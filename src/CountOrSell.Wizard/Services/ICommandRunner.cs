namespace CountOrSell.Wizard.Services;

public interface ICommandRunner
{
    bool CommandExists(string command);
    Task<(int ExitCode, string Output)> RunWithOutputAsync(string command, string arguments);
}
