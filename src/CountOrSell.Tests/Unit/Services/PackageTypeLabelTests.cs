using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// The wire value is unchanged - manifests still carry "delta" and "full" and all branching
// stays on those literals. This is only the operator-facing wording.
public class PackageTypeLabelTests
{
    [Theory]
    [InlineData("delta", "Incremental Update")]
    [InlineData("full", "Full Update")]
    [InlineData("DELTA", "Incremental Update")]
    [InlineData(" full ", "Full Update")]
    public void Maps_The_Wire_Value_To_Operator_Wording(string wire, string expected)
        => Assert.Equal(expected, PackageTypeLabel.For(wire));

    [Fact]
    public void Shows_An_Unrecognised_Type_As_Published()
    {
        // Better to surface a type this build does not know than to guess or hide it.
        Assert.Equal("snapshot", PackageTypeLabel.For("snapshot"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Falls_Back_When_The_Type_Is_Missing(string? wire)
        => Assert.Equal("Update", PackageTypeLabel.For(wire));
}
