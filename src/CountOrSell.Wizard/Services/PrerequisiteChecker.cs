using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Services;

public class PrerequisiteChecker
{
    private readonly ICommandRunner _runner;

    public PrerequisiteChecker(ICommandRunner runner)
    {
        _runner = runner;
    }

    public PrerequisiteResult CheckDockerPrerequisites()
    {
        var missing = new List<MissingPrerequisite>();

        if (!_runner.CommandExists("docker"))
        {
            missing.Add(new MissingPrerequisite("docker",
                "Docker Desktop is required. Install from https://docs.docker.com/get-docker/ - " +
                "Windows/Mac: Download and install Docker Desktop. " +
                "Linux: Follow distribution-specific instructions at the link above."));
        }

        if (!_runner.CommandExists("docker compose"))
        {
            missing.Add(new MissingPrerequisite("docker compose",
                "Docker Compose v2 is required. It is included with Docker Desktop. " +
                "Linux standalone: https://docs.docker.com/compose/install/"));
        }

        return new PrerequisiteResult(missing.Count == 0, missing);
    }

    public PrerequisiteResult CheckTerraformPrerequisites()
    {
        var missing = new List<MissingPrerequisite>();

        if (!_runner.CommandExists("terraform"))
        {
            missing.Add(new MissingPrerequisite("terraform",
                "Terraform is required. Install from https://developer.hashicorp.com/terraform/install"));
        }

        return new PrerequisiteResult(missing.Count == 0, missing);
    }

    public PrerequisiteResult CheckAzurePrerequisites()
    {
        var tf = CheckTerraformPrerequisites();
        var missing = new List<MissingPrerequisite>(tf.Missing);

        if (!_runner.CommandExists("az"))
        {
            missing.Add(new MissingPrerequisite("az",
                "Azure CLI is required. Install from https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"));
        }

        return new PrerequisiteResult(missing.Count == 0, missing);
    }

    public PrerequisiteResult CheckAwsPrerequisites()
    {
        var tf = CheckTerraformPrerequisites();
        var missing = new List<MissingPrerequisite>(tf.Missing);

        if (!_runner.CommandExists("aws"))
        {
            missing.Add(new MissingPrerequisite("aws",
                "AWS CLI is required. Install from https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html"));
        }

        if (!_runner.CommandExists("docker"))
        {
            missing.Add(new MissingPrerequisite("docker",
                "Docker Desktop is required to mirror the application image to ECR. " +
                "Install from https://docs.docker.com/get-docker/ and ensure it is running before starting the wizard."));
        }

        return new PrerequisiteResult(missing.Count == 0, missing);
    }

    public PrerequisiteResult CheckGcpPrerequisites()
    {
        var tf = CheckTerraformPrerequisites();
        var missing = new List<MissingPrerequisite>(tf.Missing);

        if (!_runner.CommandExists("gcloud"))
        {
            missing.Add(new MissingPrerequisite("gcloud",
                "Google Cloud CLI is required. Install from https://cloud.google.com/sdk/docs/install"));
        }

        return new PrerequisiteResult(missing.Count == 0, missing);
    }
}
