namespace CountOrSell.Wizard.Services;

public interface ICommandRunner
{
    bool CommandExists(string command);
}
