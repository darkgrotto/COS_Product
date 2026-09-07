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

    [Fact]
    public void Pins_The_App_Listen_Port_Rather_Than_Inheriting_It()
    {
        // PORT means the published HTTPS port in .env and Kestrel's listen port to the app.
        // Letting the .env value reach the app would leave nginx proxying to the wrong port.
        Assert.Contains("- PORT=3000", DockerComposeGenerator.Generate(Config()));
    }

    [Fact]
    public void Nginx_Proxies_To_The_Port_The_App_Actually_Listens_On()
    {
        // These two files drifted: nginx.conf still targeted 8080 - the base image's
        // ASPNETCORE_HTTP_PORTS, which the Dockerfile clears - long after the app moved to
        // 3000, so every request through the proxy returned 502. Nothing connected them.
        var yaml = DockerComposeGenerator.Generate(Config());
        var appPort = System.Text.RegularExpressions.Regex.Match(yaml, @"- PORT=(\d+)").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(appPort), "app service does not pin PORT");

        var nginxConf = File.ReadAllText(Path.Combine(RepoRoot(), "docker", "compose", "nginx.conf"));
        var upstream = System.Text.RegularExpressions.Regex.Match(nginxConf, @"proxy_pass\s+http://app:(\d+)").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(upstream), "nginx.conf has no app upstream");

        Assert.Equal(appPort, upstream);
    }

    [Fact]
    public void Scopes_Everything_Globally_Unique_To_The_Project()
    {
        // Two deployments on one host must not collide. Container names are global to the
        // Docker daemon and a pinned volume name ignores the project prefix, so both have
        // to derive from the project rather than being fixed.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains("name: ${COS_PROJECT_NAME:-countorsell}", yaml);
        Assert.Contains("container_name: ${COS_PROJECT_NAME:-countorsell}-app", yaml);
        Assert.Contains("container_name: ${COS_PROJECT_NAME:-countorsell}-postgres", yaml);
        Assert.Contains("container_name: ${COS_PROJECT_NAME:-countorsell}-reverse-proxy", yaml);
    }

    [Fact]
    public void Leaves_Volumes_Unnamed_So_Compose_Scopes_Them()
    {
        // A pinned volume name is global: every deployment on the host would attach to the
        // same database, which is how a "fresh" install came up holding five-month-old data.
        var yaml = DockerComposeGenerator.Generate(Config());
        var volumesSection = yaml[yaml.LastIndexOf("volumes:", StringComparison.Ordinal)..];

        Assert.DoesNotContain("name: countorsell_postgres_data", volumesSection);
        Assert.DoesNotContain("name: countorsell_app_data", volumesSection);
        Assert.Contains("postgres_data:", volumesSection);
        Assert.Contains("app_data:", volumesSection);
    }

    [Fact]
    public void Default_Project_Reproduces_The_Volume_Names_Deployments_Already_Have()
    {
        // Compose names a volume <project>_<key>. With the project defaulting to
        // countorsell and the keys stripped of their prefix, the result is byte-identical
        // to the previously pinned names - so an existing deployment keeps its data instead
        // of silently coming up against an empty database.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains("name: ${COS_PROJECT_NAME:-countorsell}", yaml);
        Assert.Contains("postgres_data:", yaml);   // -> countorsell_postgres_data
        Assert.Contains("app_data:", yaml);        // -> countorsell_app_data
    }

    [Fact]
    public void Resolves_The_Database_Host_By_Service_Name()
    {
        // Pointing DB_HOST at a container name breaks the moment container names are
        // scoped. Compose resolves service names on the project network regardless.
        var yaml = DockerComposeGenerator.Generate(Config());

        Assert.Contains("DB_HOST=postgres", yaml);
        Assert.DoesNotContain("DB_HOST=cos-postgres", yaml);
    }

    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
