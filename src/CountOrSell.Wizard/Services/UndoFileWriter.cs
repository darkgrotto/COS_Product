namespace CountOrSell.Wizard.Services;

/// <summary>
/// Tracks cloud provisioning actions and writes a shell script that reverses
/// them in the correct order. The file is rewritten after every step so a
/// partial wizard run always has a valid undo script.
/// </summary>
public class UndoFileWriter
{
    private readonly string _filePath;
    private readonly string _provider;
    private readonly string _instanceName;
    private readonly List<(string Description, string Command)> _steps = new();
    private bool _pathAnnounced;

    public UndoFileWriter(string filePath, string provider, string instanceName)
    {
        _filePath = filePath;
        _provider = provider;
        _instanceName = instanceName;
    }

    public string FilePath => _filePath;

    /// <summary>
    /// Records a provisioning action. The undo file is rewritten immediately
    /// so it reflects the current state even if the wizard is interrupted.
    /// </summary>
    public void AddStep(string description, string command)
    {
        _steps.Add((description, command));
        TryWriteFile();

        if (!_pathAnnounced)
        {
            Console.WriteLine($"Undo file: {_filePath}");
            _pathAnnounced = true;
        }
    }

    private void TryWriteFile()
    {
        try
        {
            var lines = new List<string>
            {
                "#!/bin/bash",
                "# CountOrSell wizard undo script",
                $"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                $"# Provider: {_provider}",
                $"# Instance: {_instanceName}",
                "#",
                "# This script reverses changes made by the CountOrSell first-run wizard.",
                "# Commands run in reverse order of the original wizard actions.",
                "# Review all commands before running.",
                "# Delete this file when it is no longer needed.",
                "",
            };

            foreach (var (description, command) in Enumerable.Reverse(_steps))
            {
                lines.Add($"echo \"Undoing: {description}\"");
                lines.Add(command);
                lines.Add("");
            }

            lines.Add("echo \"Undo complete.\"");

            File.WriteAllLines(_filePath, lines);

            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(_filePath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                }
                catch { }
            }
        }
        catch
        {
            // Non-fatal - undo file failure must never block the wizard.
        }
    }
}
