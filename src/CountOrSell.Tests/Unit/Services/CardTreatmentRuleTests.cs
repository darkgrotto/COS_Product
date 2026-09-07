using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// A card's treatments come from the update package. Pairing a card with a treatment it is
// not printed in records something that does not exist - reachable since the Backend began
// deriving treatments from promo_types, which produces printings with no regular version.
public class CardTreatmentRuleTests
{
    [Theory]
    [InlineData("foil,serialized", "foil")]
    [InlineData("foil,serialized", "serialized")]
    [InlineData("regular,foil", "regular")]
    [InlineData("surge-foil", "surge-foil")]
    public void Accepts_A_Treatment_The_Card_Is_Printed_In(string valid, string treatment)
        => Assert.True(CardTreatmentRule.Accepts(valid, treatment));

    [Theory]
    // The case that motivated this: a foil-only serialized printing has no regular version.
    [InlineData("foil,serialized", "regular")]
    [InlineData("regular", "surge-foil")]
    [InlineData("foil", "etched-foil")]
    public void Rejects_A_Treatment_The_Card_Is_Not_Printed_In(string valid, string treatment)
        => Assert.False(CardTreatmentRule.Accepts(valid, treatment));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Allows_Anything_When_The_Package_Never_Said(string? valid)
    {
        // A card we hold no treatment data for must not be blocked: that would reject
        // pre-existing entries and legacy imports without evidence the pairing is wrong.
        Assert.True(CardTreatmentRule.Accepts(valid, "regular"));
        Assert.True(CardTreatmentRule.Accepts(valid, "anything-at-all"));
    }

    [Fact]
    public void Rejects_An_Empty_Treatment_When_The_Card_Is_Constrained()
        => Assert.False(CardTreatmentRule.Accepts("foil,serialized", ""));

    [Theory]
    [InlineData("Foil,Serialized", "foil")]
    [InlineData("foil,serialized", "FOIL")]
    [InlineData(" foil , serialized ", "serialized")]
    public void Matching_Ignores_Case_And_Surrounding_Space(string valid, string treatment)
        => Assert.True(CardTreatmentRule.Accepts(valid, treatment));

    [Fact]
    public void Accepts_A_Treatment_Key_This_Build_Has_Never_Seen()
    {
        // Nothing may depend on a closed set: a treatment added upstream must pair
        // with the cards the package associates it to, with no Product release.
        Assert.True(CardTreatmentRule.Accepts("halo-foil,ripple-foil", "ripple-foil"));
        Assert.False(CardTreatmentRule.Accepts("halo-foil,ripple-foil", "confetti-foil"));
    }

    [Fact]
    public void Offered_Lists_The_Treatments_For_An_Error_Message()
    {
        Assert.Equal(new[] { "foil", "serialized" }, CardTreatmentRule.Offered("foil,serialized"));
        Assert.Equal(new[] { "foil", "serialized" }, CardTreatmentRule.Offered(" foil , serialized "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Offered_Is_Empty_When_Unconstrained(string? valid)
        => Assert.Empty(CardTreatmentRule.Offered(valid));
}
