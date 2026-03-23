using CountOrSell.Wizard.Models;
using System.Diagnostics;

namespace CountOrSell.Wizard.Steps;

public static class Step16_Deploy
{
    public static async Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 16 of 17: Deployment");
        Console.WriteLine("--------------------------");

        switch (config.DeploymentType)
        {
            case DeploymentType.Docker:
                await DeployDockerAsync(config);
                break;
            case DeploymentType.Azure:
                await DeployTerraformAsync("azure", config);
                break;
            case DeploymentType.Aws:
                await DeployTerraformAsync("aws", config);
                break;
            case DeploymentType.Gcp:
                await DeployTerraformAsync("gcp", config);
                break;
        }

        Console.WriteLine();
    }

    private static async Task DeployDockerAsync(WizardConfig config)
    {
        var baseDir = FindRepoRoot();
        var composePath = Path.Combine(baseDir, "docker", "compose", "docker-compose.yml");

        if (!File.Exists(composePath))
        {
            Console.WriteLine($"ERROR: Compose file not found at {composePath}");
            Console.WriteLine("Please ensure Step 15 completed successfully.");
            return;
        }

        Console.WriteLine("Starting Docker Compose services...");
        Console.WriteLine("Running: docker compose up -d");

        var exitCode = await RunCommandAsync("docker", $"compose -f \"{composePath}\" up -d");

        if (exitCode == 0)
        {
            Console.WriteLine("Docker Compose services started successfully.");
            Console.WriteLine($"Application available at: https://{config.Hostname}:{config.Port}");
        }
        else
        {
            Console.WriteLine($"Docker Compose exited with code {exitCode}.");
            Console.WriteLine("Check the output above for errors.");
        }
    }

    private static async Task DeployTerraformAsync(string provider, WizardConfig config)
    {
        var baseDir = FindRepoRoot();
        var tfDir = Path.Combine(baseDir, "infrastructure", provider);

        if (!Directory.Exists(tfDir))
        {
            Console.WriteLine($"ERROR: Terraform directory not found at {tfDir}");
            return;
        }

        Console.WriteLine($"Running Terraform for {provider}...");
        Console.WriteLine($"Working directory: {tfDir}");

        WriteTfvars(tfDir, provider, config);

        var backendConfig = BuildBackendConfig(provider, config);
        var initArgs = "init -input=false" + string.Concat(backendConfig.Select(kv => $" -backend-config=\"{kv.Key}={kv.Value}\""));
        var envVars = BuildCredentialEnvVars(provider, config);

        Console.WriteLine("Running: terraform init");
        int initCode = await RunCommandAsync("terraform", initArgs, tfDir, envVars);
        if (initCode != 0)
        {
            Console.WriteLine("Terraform init failed. Check output above.");
            return;
        }

        Console.WriteLine("Running: terraform apply -auto-approve");
        int applyCode = await RunCommandAsync("terraform", "apply -auto-approve -input=false -var-file=terraform.tfvars", tfDir, envVars);
        if (applyCode == 0)
        {
            Console.WriteLine("Terraform apply completed successfully.");
        }
        else
        {
            Console.WriteLine($"Terraform apply exited with code {applyCode}. Check output above.");
        }
    }

    private static void WriteTfvars(string tfDir, string provider, WizardConfig config)
    {
        var appName = SanitizeAppName(config.InstanceName);
        var dockerImage = "ghcr.io/darkgrotto/countorsell:latest";
        var lines = new List<string>
        {
            $"app_name           = \"{appName}\"",
            $"docker_image       = \"{dockerImage}\"",
            $"db_admin_username  = \"{config.DbAdminUsername}\"",
            $"db_admin_password  = \"{EscapeTfString(config.DbAdminPassword)}\"",
        };

        switch (provider)
        {
            case "azure":
                lines.Add($"subscription_id    = \"{config.CloudSubscriptionId}\"");
                lines.Add($"tenant_id          = \"{config.CloudTenantId}\"");
                lines.Add($"resource_group_name = \"{config.CloudResourceGroup}\"");
                lines.Add($"location           = \"{config.CloudRegion ?? "eastus"}\"");
                break;
            case "aws":
                lines.Add($"region             = \"{config.CloudRegion ?? "us-east-1"}\"");
                break;
            case "gcp":
                lines.Add($"project_id         = \"{config.CloudProjectId}\"");
                lines.Add($"region             = \"{config.CloudRegion ?? "us-central1"}\"");
                break;
        }

        File.WriteAllLines(Path.Combine(tfDir, "terraform.tfvars"), lines);
    }

    private static Dictionary<string, string> BuildBackendConfig(string provider, WizardConfig config)
    {
        return provider switch
        {
            "azure" => new Dictionary<string, string>
            {
                ["resource_group_name"] = config.CloudStateResourceGroup ?? string.Empty,
                ["storage_account_name"] = config.CloudStateStorageAccount ?? string.Empty,
            },
            "aws" => new Dictionary<string, string>
            {
                ["bucket"] = config.CloudStateBucket ?? string.Empty,
                ["region"] = config.CloudRegion ?? "us-east-1",
            },
            "gcp" => new Dictionary<string, string>
            {
                ["bucket"] = config.CloudStateBucket ?? string.Empty,
            },
            _ => new Dictionary<string, string>(),
        };
    }

    private static Dictionary<string, string> BuildCredentialEnvVars(string provider, WizardConfig config)
    {
        return provider switch
        {
            "aws" => new Dictionary<string, string>
            {
                ["AWS_ACCESS_KEY_ID"] = config.CloudAccessKeyId ?? string.Empty,
                ["AWS_SECRET_ACCESS_KEY"] = config.CloudSecretAccessKey ?? string.Empty,
                ["AWS_DEFAULT_REGION"] = config.CloudRegion ?? "us-east-1",
            },
            "gcp" when !string.IsNullOrEmpty(config.CloudServiceAccountKeyPath) =>
                new Dictionary<string, string>
                {
                    ["GOOGLE_APPLICATION_CREDENTIALS"] = config.CloudServiceAccountKeyPath,
                },
            _ => new Dictionary<string, string>(),
        };
    }

    private static string SanitizeAppName(string instanceName)
    {
        var name = instanceName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
        name = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrEmpty(name) ? "countorsell" : name;
    }

    private static string EscapeTfString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static async Task<int> RunCommandAsync(
        string command,
        string arguments,
        string? workingDir = null,
        Dictionary<string, string>? envVars = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        if (!string.IsNullOrEmpty(workingDir))
        {
            psi.WorkingDirectory = workingDir;
        }

        if (envVars != null)
        {
            foreach (var kv in envVars)
            {
                psi.Environment[kv.Key] = kv.Value;
            }
        }

        using var proc = Process.Start(psi);
        if (proc == null) return -1;
        await proc.WaitForExitAsync();
        return proc.ExitCode;
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
