using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// Manifests now publish an authoritative top-level version. Packages signed before that
// are never rewritten, so both shapes are in circulation until the old ones age out of
// retention - these cover reading each.
public class PackageVersionTests
{
    private static PackageManifest Manifest(
        string? version = null,
        Dictionary<string, ContentVersionEntry>? contentVersions = null,
        Dictionary<string, ContentVersionEntry>? bundledAssets = null)
        => new()
        {
            Version = version,
            ContentVersions = contentVersions ?? new(),
            BundledAssets = bundledAssets ?? new(),
        };

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

public class PackageVersionForTests
{
    private static ContentVersionEntry V(string version) => new() { Version = version };

    [Fact]
    public void Reads_The_Published_Version_Directly()
    {
        var manifest = new PackageManifest
        {
            Version = "1.5.1",
            // Deliberately inconsistent: if the top-level value were ignored and the
            // per-content versions consulted instead, this would resolve to 9.9.9.
            ContentVersions = new()
            {
                ["cards"] = V("9.9.9"), ["sets"] = V("9.9.9"), ["images"] = V("9.9.9"),
            },
        };

        Assert.Equal("1.5.1", PackageVersion.For(manifest));
    }

    [Fact]
    public void Falls_Back_To_Reconstruction_For_A_Package_Published_Before_The_Field_Existed()
    {
        // The old shape: no top-level version, keyrune still inside content_versions.
        var manifest = new PackageManifest
        {
            Version = null,
            ContentVersions = new()
            {
                ["cards"] = V("1.4.0"), ["sets"] = V("1.4.0"), ["images"] = V("1.4.0"),
                ["slabs"] = V("0.0.0"), ["keyrune"] = V("3.19.0"),
            },
        };

        Assert.Equal("1.4.0", PackageVersion.For(manifest));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Treats_A_Blank_Published_Version_As_Absent(string version)
    {
        var manifest = new PackageManifest
        {
            Version = version,
            ContentVersions = new() { ["cards"] = V("1.4.0"), ["sets"] = V("1.4.0") },
        };

        Assert.Equal("1.4.0", PackageVersion.For(manifest));
    }

    [Fact]
    public void Reads_A_Bundled_Asset_From_Its_Own_Section()
    {
        var manifest = new PackageManifest
        {
            Version = "1.5.1",
            ContentVersions = new() { ["cards"] = V("1.5.1") },
            BundledAssets = new() { ["keyrune"] = V("3.19.0") },
        };

        Assert.Equal("3.19.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));
        // The font tracks its own upstream and must never become the package version.
        Assert.Equal("1.5.1", PackageVersion.For(manifest));
    }

    [Fact]
    public void Falls_Back_To_Content_Versions_For_A_Bundled_Asset_On_An_Older_Package()
    {
        var manifest = new PackageManifest
        {
            ContentVersions = new() { ["cards"] = V("1.4.0"), ["keyrune"] = V("3.18.0") },
        };

        Assert.Equal("3.18.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));
    }

    [Fact]
    public void A_Missing_Bundled_Asset_Is_Absence_Not_An_Error()
    {
        var manifest = new PackageManifest { Version = "1.5.1", ContentVersions = new() { ["cards"] = V("1.5.1") } };

        Assert.Null(PackageVersion.BundledAssetVersion(manifest, "keyrune"));
        Assert.Null(PackageVersion.BundledAssetVersion(null, "keyrune"));
    }

    [Fact]
    public void Reads_The_Current_Published_Manifest_Shape()
    {
        // Exactly the shape the Backend now emits, slabs still pinned and keyrune moved out.
        var manifest = new PackageManifest
        {
            Version = "1.5.1",
            PackageType = "delta",
            BaseFullVersion = "1.5.0",
            SchemaVersion = "1.5.0",
            ContentVersions = new()
            {
                ["cards"] = V("1.5.1"), ["sets"] = V("1.5.1"), ["sealed_products"] = V("1.5.1"),
                ["treatments"] = V("1.5.1"), ["taxonomy"] = V("1.5.1"), ["prices"] = V("1.5.1"),
                ["slabs"] = V("0.0.0"), ["images"] = V("1.5.1"),
            },
            BundledAssets = new() { ["keyrune"] = V("3.19.0") },
        };

        Assert.Equal("1.5.1", PackageVersion.For(manifest));
        Assert.Equal("3.19.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));
    }
}
