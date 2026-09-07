using System.Reflection;
using Xunit;

namespace CountOrSell.Tests.Unit.Wizard;

// The Compose project name scopes container and volume names, so it has to be a legal
// project name whatever the operator typed as an instance name.
public class ProjectNameTests
{
    private static readonly MethodInfo Method =
        Assembly.Load("CountOrSell.Wizard")
            .GetType("CountOrSell.Wizard.Steps.Step15_GenerateFiles")!
            .GetMethod("ProjectNameFrom", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string Slug(string? instanceName) => (string)Method.Invoke(null, [instanceName])!;

    [Theory]
    [InlineData("COS Testbuild", "cos-testbuild")]
    [InlineData("My Collection", "my-collection")]
    [InlineData("UPPER", "upper")]
    [InlineData("with.dots_and-dashes", "with-dots-and-dashes")]
    [InlineData("  padded  ", "padded")]
    public void Produces_A_Legal_Project_Name(string instanceName, string expected)
        => Assert.Equal(expected, Slug(instanceName));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Falls_Back_To_The_Historical_Default(string? instanceName)
    {
        // countorsell is also what single-instance deployments already use, so the fallback
        // keeps their volume names unchanged rather than orphaning the database.
        Assert.Equal("countorsell", Slug(instanceName));
    }

    [Fact]
    public void Never_Starts_With_A_Separator()
    {
        // Compose rejects a project name that does not start with a letter or digit.
        Assert.Equal("leading", Slug("-leading"));
        Assert.Equal("countorsell", Slug("---"));
    }
}
