using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
using System.Diagnostics;

namespace CountOrSell.Wizard.Steps;

public static class Step16_Deploy
{
    public static async Task<bool> RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 16 of 17: Deployment");
        Console.WriteLine("--------------------------");

        bool success = config.DeploymentType switch
        {
            DeploymentType.Docker => await DeployDockerAsync(config),
            DeploymentType.Azure  => await DeployAzureAsync(config),
            DeploymentType.Aws    => await DeployAwsAsync(config),
            DeploymentType.Gcp    => await DeployGcpAsync(config),
            _                     => false
        };

        Console.WriteLine();
        return success;
    }

    private static async Task<bool> DeployDockerAsync(WizardConfig config)
    {
        var baseDir = FindRepoRoot();
        var composePath = Path.Combine(baseDir, "docker", "compose", "docker-compose.yml");

        if (!File.Exists(composePath))
        {
            Console.WriteLine($"ERROR: Compose file not found at {composePath}");
            Console.WriteLine("Please ensure Step 15 completed successfully.");
            return false;
        }

        Console.WriteLine("Starting Docker Compose services...");
        Console.WriteLine("Running: docker compose up -d");

        var exitCode = await RunCommandAsync("docker", $"compose -f \"{composePath}\" up -d");

        if (exitCode == 0)
        {
            Console.WriteLine("Docker Compose services started successfully.");
            Console.WriteLine($"Application available at: https://{config.Hostname}:{config.Port}");
            return true;
        }

        Console.WriteLine($"Docker Compose exited with code {exitCode}.");
        Console.WriteLine("Check the output above for errors.");
        return false;
    }

    private static async Task<bool> DeployAzureAsync(WizardConfig config)
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
            return false;
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

            Console.WriteLine($"To apply future infrastructure changes, run: {Path.Combine(baseDir, "terraform-apply.sh")}");
            Console.WriteLine($"To undo this deployment, run: {undo.FilePath}");
        }

        return applied;
    }

    private static async Task<bool> DeployAwsAsync(WizardConfig config)
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
            // Versioned buckets require purging all object versions and delete markers before
            // the bucket itself can be deleted. aws s3 rb --force only removes current versions.
            var purgeVersions =
                $"VOBJ=$(aws s3api list-object-versions --bucket {bucket}" +
                $" --query '{{Objects: Versions[].{{Key:Key,VersionId:VersionId}}, Quiet: `true`}}'" +
                $" --output json 2>/dev/null);" +
                $" echo \"$VOBJ\" | grep -q '\"Key\"' &&" +
                $" aws s3api delete-objects --bucket {bucket} --delete \"$VOBJ\" --output text 2>/dev/null";
            var purgeMarkers =
                $"MOBJ=$(aws s3api list-object-versions --bucket {bucket}" +
                $" --query '{{Objects: DeleteMarkers[].{{Key:Key,VersionId:VersionId}}, Quiet: `true`}}'" +
                $" --output json 2>/dev/null);" +
                $" echo \"$MOBJ\" | grep -q '\"Key\"' &&" +
                $" aws s3api delete-objects --bucket {bucket} --delete \"$MOBJ\" --output text 2>/dev/null";
            undo.AddStep(
                $"Delete Terraform state S3 bucket ({bucket})",
                $"{purgeVersions}; {purgeMarkers}; aws s3 rb s3://{bucket}");
        }

        int versionCode = await RunCommandAsync("aws",
            $"s3api put-bucket-versioning --bucket {bucket} " +
            "--versioning-configuration Status=Enabled");
        if (versionCode != 0)
        {
            Console.WriteLine($"WARNING: aws s3api put-bucket-versioning exited with code {versionCode}. Continuing.");
        }

        Console.WriteLine("Terraform state S3 bucket ready.");

        // Mirror the image from ghcr.io to ECR - App Runner only accepts ECR image URIs.
        var appName = SanitizeAppName(config.InstanceName);
        var ecrImageUri = await MirrorImageToEcrAsync(config, region, appName, undo);
        if (ecrImageUri == null)
        {
            Console.WriteLine("ERROR: Could not mirror image to ECR. Deployment cannot proceed.");
            return false;
        }
        config.CloudEcrImageUri = ecrImageUri;

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

            Console.WriteLine($"To apply future infrastructure changes, run: {Path.Combine(baseDir, "terraform-apply.sh")}");
            Console.WriteLine($"To undo this deployment, run: {undo.FilePath}");
        }

        return applied;
    }

    private static async Task<bool> DeployGcpAsync(WizardConfig config)
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

            Console.WriteLine($"To apply future infrastructure changes, run: {Path.Combine(baseDir, "terraform-apply.sh")}");
            Console.WriteLine($"To undo this deployment, run: {undo.FilePath}");
        }

        return applied;
    }

    private static async Task<string?> MirrorImageToEcrAsync(
        WizardConfig config, string region, string appName, UndoFileWriter undo)
    {
        Console.WriteLine("Mirroring image to ECR (App Runner requires ECR image URIs)...");

        // Verify the Docker daemon is reachable before doing anything else.
        int dockerInfoCode = await RunCommandAsync("docker", "info --format '.'");
        if (dockerInfoCode != 0)
        {
            Console.WriteLine("ERROR: Cannot connect to the Docker daemon.");
            Console.WriteLine("Start Docker Desktop and wait for it to finish starting, then retry.");
            return null;
        }

        // Resolve AWS account ID
        var (idCode, accountId) = await RunAndCaptureAsync("aws",
            "sts get-caller-identity --query Account --output text");
        if (idCode != 0 || string.IsNullOrWhiteSpace(accountId))
        {
            Console.WriteLine("ERROR: Could not determine AWS account ID.");
            return null;
        }
        accountId = accountId.Trim();

        var tag = config.DockerImageTag;
        var registryUrl = $"{accountId}.dkr.ecr.{region}.amazonaws.com";
        var ecrImageUri = $"{registryUrl}/{appName}:{tag}";

        // Create ECR repository. AlreadyExists is safe to ignore; AccessDenied means
        // the IAM credentials need ECR permissions added before the wizard can continue.
        var (repoCode, repoStderr) = await RunAndCaptureStderrAsync("aws",
            $"ecr create-repository --repository-name {appName} --region {region} --output text");
        if (repoCode == 0)
        {
            undo.AddStep(
                $"Delete ECR repository ({appName})",
                $"aws ecr delete-repository --repository-name {appName} --region {region} --force");
        }
        else if (repoStderr.Contains("AccessDeniedException") || repoStderr.Contains("not authorized"))
        {
            Console.WriteLine("ERROR: The IAM credentials are missing required ECR permissions.");
            Console.WriteLine("Add the following permissions to the IAM user or role and retry:");
            Console.WriteLine("  ecr:CreateRepository");
            Console.WriteLine("  ecr:GetAuthorizationToken");
            Console.WriteLine("  ecr:BatchCheckLayerAvailability");
            Console.WriteLine("  ecr:InitiateLayerUpload");
            Console.WriteLine("  ecr:UploadLayerPart");
            Console.WriteLine("  ecr:CompleteLayerUpload");
            Console.WriteLine("  ecr:PutImage");
            Console.WriteLine("ecr:GetAuthorizationToken requires Resource: \"*\".");
            Console.WriteLine("The remaining six can be scoped to the repository ARN:");
            Console.WriteLine($"  arn:aws:ecr:{region}:{accountId}:repository/{appName}");
            return null;
        }
        else
        {
            Console.WriteLine("NOTE: ECR repository may already exist. Continuing.");
        }

        // Authenticate Docker to ECR
        var (tokenCode, loginToken) = await RunAndCaptureAsync("aws",
            $"ecr get-login-password --region {region}");
        if (tokenCode != 0 || string.IsNullOrWhiteSpace(loginToken))
        {
            Console.WriteLine("ERROR: Could not retrieve ECR login token.");
            Console.WriteLine("Ensure the IAM credentials include ecr:GetAuthorizationToken on Resource: \"*\".");
            return null;
        }

        int loginCode = await RunCommandWithInputAsync("docker",
            $"login --username AWS --password-stdin {registryUrl}", loginToken.Trim());
        if (loginCode != 0)
        {
            Console.WriteLine("ERROR: Docker login to ECR failed.");
            return null;
        }

        // Pull from ghcr.io, tag, and push to ECR.
        var sourceImage = $"ghcr.io/darkgrotto/countorsell:{tag}";

        // Authenticate to ghcr.io via the GitHub CLI token when available.
        // This enables pulling private packages; public packages also work this way.
        bool ghcrAuthenticated = false;
        var (ghTokenCode, ghToken) = await RunAndCaptureAsync("gh", "auth token");
        if (ghTokenCode == 0 && !string.IsNullOrWhiteSpace(ghToken))
        {
            var (ghUserCode, ghUser) = await RunAndCaptureAsync("gh", "api user --jq .login");
            var loginUser = (ghUserCode == 0 && !string.IsNullOrWhiteSpace(ghUser))
                ? ghUser.Trim()
                : "gh";
            int ghLoginCode = await RunCommandWithInputAsync("docker",
                $"login ghcr.io --username {loginUser} --password-stdin", ghToken.Trim());
            ghcrAuthenticated = ghLoginCode == 0;
            if (ghcrAuthenticated)
                Console.WriteLine("Authenticated with ghcr.io via GitHub CLI.");
            else
                Console.WriteLine("WARNING: ghcr.io login failed. Attempting anonymous pull.");
        }
        else
        {
            Console.WriteLine("GitHub CLI not logged in. Attempting anonymous pull (public images only).");
            // Log out to clear any stale credentials that could produce a misleading error.
            await RunCommandAsync("docker", "logout ghcr.io");
        }

        Console.WriteLine($"Pulling {sourceImage} ...");
        var (pullCode, pullStderr) = await RunAndCaptureStderrAsync("docker", $"pull {sourceImage}");
        if (pullCode != 0)
        {
            Console.WriteLine($"ERROR: docker pull {sourceImage} failed.");
            if (ghcrAuthenticated && pullStderr.Contains("403"))
            {
                Console.WriteLine("The GitHub token is missing the read:packages scope.");
                Console.WriteLine("Run the following, then retry the wizard:");
                Console.WriteLine("  gh auth refresh -s read:packages");
            }
            else if (ghcrAuthenticated)
            {
                Console.WriteLine($"  - Verify that tag \"{tag}\" has been published to ghcr.io/darkgrotto/countorsell.");
            }
            else
            {
                Console.WriteLine("Likely causes:");
                Console.WriteLine($"  - The image tag \"{tag}\" has not been published yet.");
                Console.WriteLine("  - The image is private and requires authentication.");
                Console.WriteLine("Log in with the GitHub CLI and retry:");
                Console.WriteLine("  gh auth login");
            }
            return null;
        }

        int tagCode = await RunCommandAsync("docker", $"tag {sourceImage} {ecrImageUri}");
        if (tagCode != 0)
        {
            Console.WriteLine("ERROR: docker tag failed.");
            return null;
        }

        Console.WriteLine($"Pushing to {ecrImageUri} ...");
        var (pushCode, pushStderr) = await RunAndCaptureStderrAsync("docker", $"push {ecrImageUri}");
        if (pushCode != 0)
        {
            Console.WriteLine("ERROR: docker push failed.");
            if (pushStderr.Contains("403"))
            {
                Console.WriteLine("The IAM credentials are missing ecr:PutImage permission.");
                Console.WriteLine("Add ecr:PutImage to the ECR policy for this repository and retry.");
                Console.WriteLine("See docs/deployment/credentials.md for the full required policy.");
            }
            return null;
        }

        Console.WriteLine("Image mirrored to ECR successfully.");
        return ecrImageUri;
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
        var dockerImage = provider == "aws" && config.CloudEcrImageUri != null
            ? config.CloudEcrImageUri
            : $"ghcr.io/darkgrotto/countorsell:{config.DockerImageTag}";
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

    private static async Task<int> RunCommandWithInputAsync(
        string command,
        string arguments,
        string input)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        using var proc = Process.Start(psi);
        if (proc == null) return -1;
        await proc.StandardInput.WriteAsync(input);
        proc.StandardInput.Close();
        await proc.WaitForExitAsync();
        return proc.ExitCode;
    }

    // Captures stderr while letting stdout flow to the console.
    // Used to inspect error text (e.g. to distinguish AccessDenied from AlreadyExists)
    // without suppressing visible command output.
    private static async Task<(int ExitCode, string Stderr)> RunAndCaptureStderrAsync(
        string command,
        string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        using var proc = Process.Start(psi);
        if (proc == null) return (-1, string.Empty);
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (stderr.Length > 0) Console.Error.Write(stderr);
        return (proc.ExitCode, stderr);
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
