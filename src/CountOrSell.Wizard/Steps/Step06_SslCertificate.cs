using CountOrSell.Wizard.Models;
using System.Diagnostics;

namespace CountOrSell.Wizard.Steps;

public static class Step06_SslCertificate
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 6 of 17: SSL Certificate");
        Console.WriteLine("------------------------------");

        if (config.DeploymentType != DeploymentType.Docker)
        {
            Console.WriteLine("SSL is handled natively by your cloud provider.");
            Console.WriteLine("No certificate generation required.");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        Console.WriteLine("Generating a self-signed SSL certificate for Docker deployment...");

        var certsDir = GetCertsDirectory();
        Directory.CreateDirectory(certsDir);

        var certPath = Path.Combine(certsDir, "countorsell.crt");
        var keyPath = Path.Combine(certsDir, "countorsell.key");

        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            Console.WriteLine("Existing certificate found. Using existing certificate.");
            Console.WriteLine($"Certificate: {certPath}");
            Console.WriteLine($"Key: {keyPath}");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        bool generated = TryGenerateWithOpenSsl(config.Hostname, certPath, keyPath);

        if (generated)
        {
            Console.WriteLine("Self-signed certificate generated.");
            Console.WriteLine($"Certificate: {certPath}");
            Console.WriteLine($"Key: {keyPath}");
        }
        else
        {
            Console.WriteLine("Could not auto-generate certificate (openssl not found).");
            Console.WriteLine("Please place your certificate files at:");
            Console.WriteLine($"  {certPath}");
            Console.WriteLine($"  {keyPath}");
            Console.WriteLine();
            Console.Write("Press Enter once files are in place to continue...");
            Console.ReadLine();
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    private static bool TryGenerateWithOpenSsl(string hostname, string certPath, string keyPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = $"req -x509 -newkey rsa:4096 -keyout \"{keyPath}\" " +
                            $"-out \"{certPath}\" -days 365 -nodes " +
                            $"-subj \"/CN={hostname}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return false;
            proc.WaitForExit(30000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string GetCertsDirectory()
    {
        // Place certs relative to the wizard executable
        var baseDir = AppContext.BaseDirectory;
        // Walk up to find docker/certs
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && dir.Parent != null)
        {
            var certsCandidate = Path.Combine(dir.FullName, "docker", "certs");
            if (Directory.Exists(Path.GetDirectoryName(certsCandidate)!))
            {
                return certsCandidate;
            }
            dir = dir.Parent;
        }

        return Path.Combine(baseDir, "certs");
    }
}
