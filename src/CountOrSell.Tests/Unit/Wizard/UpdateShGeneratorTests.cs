using CountOrSell.Wizard.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Wizard;

// update.sh is the operator's only supported way to update a Docker deployment, so its
// generated text is worth asserting on directly - every defect in it so far only appeared
// when someone ran it.
public class UpdateShGeneratorTests
{
    private static readonly string Script = UpdateShGenerator.Generate();

    [Fact]
    public void Points_At_The_Deployment_Root_Env_File()
    {
        // Compose resolves .env from the Compose file's own directory. The wizard writes it
        // two levels up, so without --env-file every variable expands blank - which would
        // recreate the containers with an empty database password.
        Assert.Contains("ENV_FILE=\"$SCRIPT_DIR/../../.env\"", Script);
        Assert.Contains("--env-file \"$ENV_FILE\"", Script);
    }

    [Fact]
    public void Continues_Without_An_Env_File_Rather_Than_Aborting()
    {
        Assert.Contains("if [ -f \"$ENV_FILE\" ]; then", Script);
        Assert.Contains("Warning:", Script);
    }

    [Fact]
    public void Avoids_An_Args_Array_That_Breaks_On_Bash_3()
    {
        // An empty array under `set -u` is an unbound variable on bash 3.2, which macOS
        // still ships - the array form would trade one failure for another.
        Assert.DoesNotContain("[@]}", Script);
        Assert.Contains("compose() {", Script);
    }

    [Fact]
    public void Runs_Both_Update_Steps_Through_The_Env_Aware_Wrapper()
    {
        Assert.Contains("compose pull", Script);
        Assert.Contains("compose up -d", Script);
        // The bare form would bypass --env-file entirely.
        Assert.DoesNotContain("docker compose -f \"$COMPOSE_FILE\" pull", Script);
    }

    [Fact]
    public void Is_Valid_Shell_Under_The_Bash_That_MacOS_Ships()
    {
        // bash 3.2 is stricter than the bash most contributors develop against.
        var path = Path.Combine(Path.GetTempPath(), $"update-{Guid.NewGuid():N}.sh");
        File.WriteAllText(path, Script);
        try
        {
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-n \"{path}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
            })!;
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, $"generated script is not valid bash: {stderr}");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
