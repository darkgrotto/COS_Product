using CountOrSell.Domain;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

public class CardTypeLineParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Land")]
    [InlineData("Artifact")]
    [InlineData("Legendary Creature")] // no dash, no subtypes
    public void ExtractSubtypes_NoSubtypes_ReturnsNull(string? input)
    {
        Assert.Null(CardTypeLineParser.ExtractSubtypes(input));
    }

    [Theory]
    [InlineData("Basic Land — Forest",                  "Forest")]
    [InlineData("Creature — Human",                     "Human")]
    [InlineData("Legendary Creature — Human Wizard",    "Human,Wizard")]
    [InlineData("Artifact — Equipment",                 "Equipment")]
    [InlineData("Artifact Creature — Alien Soldier",    "Alien,Soldier")]
    [InlineData("Legendary Planeswalker — Bolas",       "Bolas")]
    public void ExtractSubtypes_SingleFace_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, CardTypeLineParser.ExtractSubtypes(input));
    }

    [Theory]
    [InlineData("Artifact // Creature — Demon",                            "Demon")]
    [InlineData("Artifact // Artifact Creature — Bird Construct",          "Bird,Construct")]
    [InlineData("Creature — Human Wizard // Creature — Spirit Wizard","Human,Wizard,Spirit")]
    public void ExtractSubtypes_DoubleFaced_DeduplicatesAndPreservesOrder(string input, string expected)
    {
        Assert.Equal(expected, CardTypeLineParser.ExtractSubtypes(input));
    }

    [Fact]
    public void ExtractSubtypes_TrailingWhitespace_Trimmed()
    {
        Assert.Equal("Forest", CardTypeLineParser.ExtractSubtypes("Basic Land —   Forest   "));
    }
}
