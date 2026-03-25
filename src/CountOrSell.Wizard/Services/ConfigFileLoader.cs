using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Services;

public static class ConfigFileLoader
{
    public static void Load(WizardConfig config)
    {
        var fileName = config.DeploymentType switch
        {
            DeploymentType.Azure  => "wizard-azure.conf",
            DeploymentType.Aws    => "wizard-aws.conf",
            DeploymentType.Gcp    => "wizard-gcp.conf",
            DeploymentType.Docker => "wizard-docker.conf",
            _                     => null
        };

        if (fileName == null) return;

        var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        if (!File.Exists(path)) return;

        var values = ParseFile(path);
        if (values.Count == 0) return;

        foreach (var kv in values)
        {
            config.ConfigValues[kv.Key] = kv.Value;
        }

        Console.WriteLine($"Loaded {values.Count} value(s) from {fileName}.");
        Console.WriteLine("Passwords are never read from configuration files.");
        Console.WriteLine();
    }

    private static Dictionary<string, string> ParseFile(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;
            var idx = trimmed.IndexOf('=');
            if (idx < 1) continue;
            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result[key] = value;
        }
        return result;
    }
}
