using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Wizard;

// The wizard's generated files had no coverage, and every defect in them surfaced only when
// someone ran the output: a secret baked into a tracked file, an image reference that
// doubled the repository name, and variables with no default that expand to something
// Docker rejects. These assert on the emitted text.
public class DockerComposeGeneratorTests
{
    private static WizardConfig Config() => new()
    {
        DockerRegistry = "ghcr.io/darkgrotto/countorsell",
        DockerImageTag = "latest",
        InstanceName = "Test Instance",
        DbAdminUsername = "cosadmin",
        DbAdminPassword = "correct-horse-battery-staple",
        BackupConnectionString = "DefaultEndpointsProtocol=https;AccountKey=SUPERSECRET==",
        SetupToken = "setup-token-value",
        Port = 8443,
    };

    [Fact]
    public void Emits_No_Credentials_Into_The_Tracked_Compose_File()
    {
        // docker/compose/ is tracked in git and .gitignore covers only .env, so a secret
        // interpolated here is one `git add` away from being published.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.DoesNotContain("correct-horse-battery-staple", yaml);
        Assert.DoesNotContain("SUPERSECRET", yaml);
        Assert.DoesNotContain("setup-token-value", yaml);
    }

    [Fact]
    public void Passes_Secrets_As_Variable_References()
    {
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains("DB_PASSWORD=${DB_PASSWORD}", yaml);
        Assert.Contains("SETUP_TOKEN=${SETUP_TOKEN}", yaml);
        Assert.Contains("BLOB_BACKUP_CONNECTION=${BLOB_BACKUP_CONNECTION}", yaml);
    }

    [Fact]
    public void Does_Not_Double_The_Repository_Name_In_The_Image_Reference()
    {
        // The Step 3 prompt collects the full repository path, so composing it with a
        // separate image name would produce ".../countorsell/countorsell".
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.DoesNotContain("countorsell/countorsell", yaml);
        Assert.Contains("${APP_IMAGE:-ghcr.io/darkgrotto/countorsell:latest}", yaml);
    }

    [Theory]
    [InlineData("APP_IMAGE")]
    [InlineData("PORT")]
    public void Variables_Whose_Absence_Would_Break_Compose_Carry_Defaults(string key)
    {
        // An empty APP_IMAGE or PORT yields "" and ":443" - Docker rejects both outright
        // rather than falling back, which is how this surfaced originally.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains($"${{{key}:-", yaml);
    }

    [Fact]
    public void Leaves_The_Healthcheck_Expansion_To_The_Container_Shell()
    {
        // $$ is Compose's escape: a single $ here would be interpolated at compose time,
        // leaving the healthcheck running against an empty user and database.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains("pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB", yaml);
    }

    [Fact]
    public void Output_Matches_The_File_Tracked_In_The_Repository()
    {
        // The generator and the committed artifact had silently diverged into two
        // incompatible designs. Byte-comparing them is what stops that recurring.
        var tracked = Path.Combine(RepoRoot(), "docker", "compose", "docker-compose.yml");
        Assert.True(File.Exists(tracked), $"tracked Compose file not found at {tracked}");

        Assert.Equal(
            File.ReadAllText(tracked).ReplaceLineEndings("\n"),
            DockerComposeGenerator.Generate(Config()).ReplaceLineEndings("\n"));
    }

    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
