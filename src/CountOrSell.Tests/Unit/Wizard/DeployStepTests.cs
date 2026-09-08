using System.Reflection;
using Xunit;

namespace CountOrSell.Tests.Unit.Wizard;

// The deploy step's shape is worth asserting because its defects only appear when a person
// runs the wizard - and it is the one path that has no dry run.
public class DeployStepTests
{
    private static readonly string Source = ReadSource();

    private static string ReadSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "CountOrSell.Wizard", "Steps", "Step15_Deploy.cs"));
    }

    [Fact]
    public void Pulls_Before_Bringing_The_Stack_Up()
    {
        // Compose's default pull policy is "missing", so an image cached locally is reused
        // however stale. Without an explicit pull a first run can deploy a days-old image
        // and report success, with the running app quietly reporting an older version.
        var pullIndex = Source.IndexOf("var pullCode = await RunCommandAsync", StringComparison.Ordinal);
        var upIndex = Source.IndexOf("var exitCode = await RunCommandAsync", StringComparison.Ordinal);

        Assert.True(pullIndex >= 0, "deploy step does not pull images");
        Assert.True(upIndex >= 0, "deploy step does not bring the stack up");
        Assert.True(pullIndex < upIndex, "pull must run before up -d");
    }

    [Fact]
    public void A_Failed_Pull_Does_Not_Abort_The_Deployment()
    {
        // A cached image is still deployable offline, and `up` fails clearly on its own if
        // the image is genuinely absent - so a pull failure warns rather than stopping.
        Assert.Contains("WARNING: could not pull the latest images", Source);
    }

    [Fact]
    public void Both_Compose_Invocations_Pass_The_Env_File()
    {
        // Compose resolves .env from the Compose file's directory, not the deployment root,
        // so an invocation missing --env-file runs with every variable blank.
        var invocations = System.Text.RegularExpressions.Regex.Matches(Source, @"\$""compose \{envArg\}");
        Assert.True(invocations.Count >= 2,
            $"expected pull and up to both pass the env file, found {invocations.Count}");
    }

    [Fact]
    public void A_Blocked_Setup_Is_Reported_Rather_Than_Passed_Over()
    {
        // Returning success here left the operator with credentials that were never created.
        Assert.Contains("WARNING: The accounts you entered were NOT created.", Source);
        Assert.Contains("AccountsCreated = false;", Source);
    }
}
