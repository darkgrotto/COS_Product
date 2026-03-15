using CountOrSell.Wizard.Services;
using Moq;
using Xunit;

namespace CountOrSell.Tests.WizardTests;

public class PrerequisiteDetectionTests
{
    [Fact]
    public void DockerPrerequisiteCheck_WhenDockerMissing_ReturnsInstallInstructions()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("docker")).Returns(false);
        mock.Setup(r => r.CommandExists("docker compose")).Returns(false);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckDockerPrerequisites();

        Assert.False(result.AllMet);
        Assert.Contains(result.Missing, m => m.Name == "docker");
        Assert.All(result.Missing, m => Assert.NotEmpty(m.InstallInstructions));
    }

    [Fact]
    public void DockerPrerequisiteCheck_WhenAllPresent_ReturnsAllMet()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("docker")).Returns(true);
        mock.Setup(r => r.CommandExists("docker compose")).Returns(true);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckDockerPrerequisites();

        Assert.True(result.AllMet);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void DockerPrerequisiteCheck_WhenOnlyDockerMissing_ReportsDockerOnly()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("docker")).Returns(false);
        mock.Setup(r => r.CommandExists("docker compose")).Returns(true);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckDockerPrerequisites();

        Assert.False(result.AllMet);
        Assert.Single(result.Missing);
        Assert.Equal("docker", result.Missing[0].Name);
    }

    [Fact]
    public void DockerPrerequisiteCheck_WhenOnlyComposeMissing_ReportsComposeOnly()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("docker")).Returns(true);
        mock.Setup(r => r.CommandExists("docker compose")).Returns(false);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckDockerPrerequisites();

        Assert.False(result.AllMet);
        Assert.Single(result.Missing);
        Assert.Equal("docker compose", result.Missing[0].Name);
    }

    [Fact]
    public void DockerPrerequisiteCheck_WhenBothMissing_ReportsBoth()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("docker")).Returns(false);
        mock.Setup(r => r.CommandExists("docker compose")).Returns(false);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckDockerPrerequisites();

        Assert.False(result.AllMet);
        Assert.Equal(2, result.Missing.Count);
    }

    [Fact]
    public void AzurePrerequisiteCheck_WhenAzCliMissing_ReturnsInstallInstructions()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("terraform")).Returns(true);
        mock.Setup(r => r.CommandExists("az")).Returns(false);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckAzurePrerequisites();

        Assert.False(result.AllMet);
        Assert.Contains(result.Missing, m => m.Name == "az");
        Assert.All(result.Missing, m => Assert.NotEmpty(m.InstallInstructions));
    }

    [Fact]
    public void AzurePrerequisiteCheck_WhenAllPresent_ReturnsAllMet()
    {
        var mock = new Mock<ICommandRunner>();
        mock.Setup(r => r.CommandExists("terraform")).Returns(true);
        mock.Setup(r => r.CommandExists("az")).Returns(true);

        var checker = new PrerequisiteChecker(mock.Object);
        var result = checker.CheckAzurePrerequisites();

        Assert.True(result.AllMet);
        Assert.Empty(result.Missing);
    }
}
