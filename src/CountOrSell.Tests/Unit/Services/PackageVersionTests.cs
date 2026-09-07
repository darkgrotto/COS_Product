using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// A manifest carries a version per content type, not one for the package. These cover
// picking the value that actually describes the package.
public class PackageVersionTests
{
    private static Dictionary<string, ContentVersionEntry> Versions(params (string Key, string Version)[] entries)
        => entries.ToDictionary(e => e.Key, e => new ContentVersionEntry { Version = e.Version });

    [Fact]
    public void Resolves_The_Version_A_Real_Manifest_Stamps_Across_Its_Content()
    {
        // Shape taken from the live 20260907-055037-e557b8 delta: every real content type
        // on 1.5.1, slabs pinned at 0.0.0, keyrune on the font's own upstream version.
        var versions = Versions(
            ("cards", "1.5.1"), ("sets", "1.5.1"), ("sealed_products", "1.5.1"),
            ("treatments", "1.5.1"), ("taxonomy", "1.5.1"), ("prices", "1.5.1"),
            ("images", "1.5.1"), ("slabs", "0.0.0"), ("keyrune", "3.19.0"));

        Assert.Equal("1.5.1", PackageVersion.Resolve(versions));
    }

    [Fact]
    public void Is_Not_Fooled_By_The_Slabs_Placeholder()
    {
        // slabs is Product-managed and never published, so it is always 0.0.0 - reporting
        // that as the package version would be wrong on every single update.
        var versions = Versions(("cards", "2.0.0"), ("sets", "2.0.0"), ("slabs", "0.0.0"));
        Assert.Equal("2.0.0", PackageVersion.Resolve(versions));
    }

    [Fact]
    public void Is_Not_Fooled_By_An_Independently_Versioned_Entry()
    {
        // keyrune tracks the font upstream. Nothing names it here: it loses because the
        // real content types outnumber it, which keeps working if another such key appears.
        var versions = Versions(("cards", "1.6.0"), ("images", "1.6.0"), ("keyrune", "9.9.9"));
        Assert.Equal("1.6.0", PackageVersion.Resolve(versions));
    }

    [Fact]
    public void Prefers_Cards_When_Two_Versions_Appear_Equally_Often()
    {
        var versions = Versions(("cards", "1.5.1"), ("keyrune", "3.19.0"));
        Assert.Equal("1.5.1", PackageVersion.Resolve(versions));
    }

    [Fact]
    public void Returns_Null_When_There_Is_Nothing_To_Report()
    {
        // The notice omits the version rather than inventing one.
        Assert.Null(PackageVersion.Resolve(null));
        Assert.Null(PackageVersion.Resolve(new Dictionary<string, ContentVersionEntry>()));
        Assert.Null(PackageVersion.Resolve(Versions(("cards", ""), ("sets", "   "))));
    }
}
