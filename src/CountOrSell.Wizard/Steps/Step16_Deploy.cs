using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
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
                await DeployAzureAsync(config);
                break;
            case DeploymentType.Aws:
                await DeployAwsAsync(config);
                break;
            case DeploymentType.Gcp:
                await DeployGcpAsync(config);
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

    private static async Task DeployAzureAsync(WizardConfig config)
    {
        var region = config.CloudRegion ?? "eastus";
        var stateRg = config.CloudStateResourceGroup ?? "countorsell-tfstate-rg";
        var stateAccount = config.CloudStateStorageAccount ?? string.Empty;
        var baseDir = FindRepoRoot();
        var tfDir = Path.Combine(baseDir, "infrastructure", "azure");

        var undo = new UndoFileWriter(
            Path.Combine(baseDir, $"countorsell-undo-azure-{DateTime.UtcNow:yyyyMMdd-HHmmss}.sh"),
            "Azure",
            config.InstanceName);

        Console.WriteLine("Provisioning Terraform state storage...");

        int rgCode = await RunCommandAsync("az",
            $"group create --name {stateRg} --location {region} --output none");
        if (rgCode != 0)
        {
            Console.WriteLine($"WARNING: az group create exited with code {rgCode}. Continuing.");
        }
        else
        {
            undo.AddStep(
                $"Delete Terraform state resource group ({stateRg})",
                $"az group delete --name {stateRg} --yes --no-wait");
        }

        int accountCode = await RunCommandAsync("az",
            $"storage account create --name {stateAccount} --resource-group {stateRg} " +
            $"--location {region} --sku Standard_LRS --allow-blob-public-access false --output none");
        if (accountCode != 0)
        {
            Console.WriteLine($"WARNING: az storage account create exited with code {accountCode}. Continuing.");
        }
        else
        {
            undo.AddStep(
                $"Delete Terraform state storage account ({stateAccount})",
                $"az storage account delete --name {stateAccount} --resource-group {stateRg} --yes");
        }

        var (connCode, connectionString) = await RunAndCaptureAsync("az",
            $"storage account show-connection-string --name {stateAccount} " +
            $"--resource-group {stateRg} --output tsv --query connectionString");
        if (connCode != 0 || string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: Could not retrieve storage account connection string.");
            return;
        }

        int containerCode = await RunCommandAsync("az",
            $"storage container create --name tfstate --connection-string \"{connectionString}\" --output none");
        if (containerCode != 0)
        {
            Console.WriteLine($"WARNING: az storage container create exited with code {containerCode}. Continuing.");
        }
        else
        {
            undo.AddStep(
                "Delete Terraform state storage container (tfstate)",
                $"ACCOUNT_KEY=$(az storage account keys list --account-name {stateAccount} " +
                $"--resource-group {stateRg} --query '[0].value' --output tsv) && " +
                $"az storage container delete --name tfstate --account-name {stateAccount} --account-key \"$ACCOUNT_KEY\"");
        }

        Console.WriteLine("Terraform state storage ready.");

        var backendConfig = new Dictionary<string, string>
        {
            ["resource_group_name"] = stateRg,
            ["storage_account_name"] = stateAccount,
        };

        bool applied = await RunTerraformAsync("azure", config, backendConfig, tfDir);
        if (applied)
        {
            var initFlags = string.Concat(backendConfig.Select(kv => $" -backend-config=\"{kv.Key}={kv.Value}\""));
            undo.AddStep(
                "Destroy Terraform-managed Azure infrastructure",
                $"cd \"{tfDir}\" && terraform init -input=false{initFlags} && " +
                "terraform destroy -auto-approve -input=false -var-file=terraform.tfvars");

            Console.WriteLine($"If you need to undo this deployment, run: {undo.FilePath}");
        }
    }

    private static async Task DeployAwsAsync(WizardConfig config)
    {
        var region = config.CloudRegion ?? "us-east-1";
        var bucket = config.CloudStateBucket ?? string.Empty;
        var baseDir = FindRepoRoot();
        var tfDir = Path.Combine(baseDir, "infrastructure", "aws");

        var undo = new UndoFileWriter(
            Path.Combine(baseDir, $"countorsell-undo-aws-{DateTime.UtcNow:yyyyMMdd-HHmmss}.sh"),
            "AWS",
            config.InstanceName);

        Console.WriteLine("Provisioning Terraform state S3 bucket...");

        var createArgs = region == "us-east-1"
            ? $"s3api create-bucket --bucket {bucket} --region {region}"
            : $"s3api create-bucket --bucket {bucket} --region {region} " +
              $"--create-bucket-configuration LocationConstraint={region}";

        int bucketCode = await RunCommandAsync("aws", createArgs);
        if (bucketCode != 0)
        {
            Console.WriteLine($"WARNING: aws s3api create-bucket exited with code {bucketCode}. " +
                "If the bucket already exists and belongs to your account, this is safe to ignore.");
        }
        else
        {
            undo.AddStep(
                $"Delete Terraform state S3 bucket ({bucket})",
                $"aws s3 rb s3://{bucket} --force");
        }

        int versionCode = await RunCommandAsync("aws",
            $"s3api put-bucket-versioning --bucket {bucket} " +
            "--versioning-configuration Status=Enabled");
        if (versionCode != 0)
        {
            Console.WriteLine($"WARNING: aws s3api put-bucket-versioning exited with code {versionCode}. Continuing.");
        }

        Console.WriteLine("Terraform state S3 bucket ready.");

        var backendConfig = new Dictionary<string, string>
        {
            ["bucket"] = bucket,
            ["region"] = region,
        };

        bool applied = await RunTerraformAsync("aws", config, backendConfig, tfDir);
        if (applied)
        {
            var initFlags = string.Concat(backendConfig.Select(kv => $" -backend-config=\"{kv.Key}={kv.Value}\""));
            undo.AddStep(
                "Destroy Terraform-managed AWS infrastructure",
                $"cd \"{tfDir}\" && terraform init -input=false{initFlags} && " +
                "terraform destroy -auto-approve -input=false -var-file=terraform.tfvars");

            Console.WriteLine($"If you need to undo this deployment, run: {undo.FilePath}");
        }
    }

    private static async Task DeployGcpAsync(WizardConfig config)
    {
        var project = config.CloudProjectId ?? string.Empty;
        var region = config.CloudRegion ?? "us-central1";
        var bucket = config.CloudStateBucket ?? string.Empty;
        var baseDir = FindRepoRoot();
        var tfDir = Path.Combine(baseDir, "infrastructure", "gcp");

        var undo = new UndoFileWriter(
            Path.Combine(baseDir, $"countorsell-undo-gcp-{DateTime.UtcNow:yyyyMMdd-HHmmss}.sh"),
            "GCP",
            config.InstanceName);

        Console.WriteLine("Enabling required GCP APIs...");
        int apiCode = await RunCommandAsync("gcloud",
            $"services enable run.googleapis.com sqladmin.googleapis.com " +
            $"secretmanager.googleapis.com storage.googleapis.com " +
            $"iam.googleapis.com cloudresourcemanager.googleapis.com " +
            $"--project {project}");
        if (apiCode != 0)
        {
            Console.WriteLine($"WARNING: gcloud services enable exited with code {apiCode}. Continuing.");
        }

        Console.WriteLine("Provisioning Terraform state GCS bucket...");

        int bucketCode = await RunCommandAsync("gcloud",
            $"storage buckets create gs://{bucket} --project {project} --location {region}");
        if (bucketCode != 0)
        {
            Console.WriteLine($"WARNING: gcloud storage buckets create exited with code {bucketCode}. " +
                "If the bucket already exists, this is safe to ignore.");
        }
        else
        {
            undo.AddStep(
                $"Delete Terraform state GCS bucket ({bucket})",
                $"gcloud storage rm -r gs://{bucket}");
        }

        int versionCode = await RunCommandAsync("gcloud",
            $"storage buckets update gs://{bucket} --versioning");
        if (versionCode != 0)
        {
            Console.WriteLine($"WARNING: gcloud storage buckets update exited with code {versionCode}. Continuing.");
        }

        Console.WriteLine("Terraform state GCS bucket ready.");

        var backendConfig = new Dictionary<string, string>
        {
            ["bucket"] = bucket,
        };

        bool applied = await RunTerraformAsync("gcp", config, backendConfig, tfDir);
        if (applied)
        {
            var initFlags = string.Concat(backendConfig.Select(kv => $" -backend-config=\"{kv.Key}={kv.Value}\""));
            undo.AddStep(
                "Destroy Terraform-managed GCP infrastructure",
                $"cd \"{tfDir}\" && terraform init -input=false{initFlags} && " +
                "terraform destroy -auto-approve -input=false -var-file=terraform.tfvars");

            Console.WriteLine($"If you need to undo this deployment, run: {undo.FilePath}");
        }
    }

    private static async Task<bool> RunTerraformAsync(
        string provider,
        WizardConfig config,
        Dictionary<string, string> backendConfig,
        string tfDir)
    {
        if (!Directory.Exists(tfDir))
        {
            Console.WriteLine($"ERROR: Terraform directory not found at {tfDir}");
            return false;
        }

        Console.WriteLine($"Running Terraform for {provider}...");
        Console.WriteLine($"Working directory: {tfDir}");

        WriteTfvars(tfDir, provider, config);

        var initArgs = "init -input=false" +
            string.Concat(backendConfig.Select(kv => $" -backend-config=\"{kv.Key}={kv.Value}\""));

        Console.WriteLine("Running: terraform init");
        int initCode = await RunCommandAsync("terraform", initArgs, tfDir);
        if (initCode != 0)
        {
            Console.WriteLine("Terraform init failed. Check output above.");
            return false;
        }

        Console.WriteLine("Running: terraform apply -auto-approve");
        int applyCode = await RunCommandAsync("terraform",
            "apply -auto-approve -input=false -var-file=terraform.tfvars", tfDir);
        if (applyCode == 0)
        {
            Console.WriteLine("Terraform apply completed successfully.");
            return true;
        }

        Console.WriteLine($"Terraform apply exited with code {applyCode}. Check output above.");
        return false;
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
        string? workingDir = null)
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

    private static async Task<(int ExitCode, string Output)> RunAndCaptureAsync(
        string command,
        string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return (-1, string.Empty);
        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (proc.ExitCode, output.Trim());
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
