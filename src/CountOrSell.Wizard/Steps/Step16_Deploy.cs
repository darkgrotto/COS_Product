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
                await DeployTerraformAsync("azure");
                break;
            case DeploymentType.Aws:
                await DeployTerraformAsync("aws");
                break;
            case DeploymentType.Gcp:
                await DeployTerraformAsync("gcp");
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

    private static async Task DeployTerraformAsync(string provider)
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

        Console.WriteLine("Running: terraform init");
        int initCode = await RunCommandAsync("terraform", "init", tfDir);
        if (initCode != 0)
        {
            Console.WriteLine("Terraform init failed. Check output above.");
            return;
        }

        Console.WriteLine("Running: terraform apply -auto-approve");
        int applyCode = await RunCommandAsync("terraform", "apply -auto-approve", tfDir);
        if (applyCode == 0)
        {
            Console.WriteLine("Terraform apply completed successfully.");
        }
        else
        {
            Console.WriteLine($"Terraform apply exited with code {applyCode}. Check output above.");
        }
    }

    private static async Task<int> RunCommandAsync(string command, string arguments, string? workingDir = null)
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
