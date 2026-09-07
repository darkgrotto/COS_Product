using System.Text.Json;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Services;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// Both manifest shapes are in circulation: already-published packages are never rewritten,
// so packages signed before the top-level version existed keep the old shape until they age
// out of retention. These parse real JSON rather than constructing DTOs, so they also cover
// the deserializer tolerating fields this build does not model.
public class ManifestShapeTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private const string CurrentShape = """
    {
      "version": "1.5.1",
      "package_type": "delta",
      "generated_at": "2026-09-07T05:51:26.5383000+00:00",
      "base_full_version": "1.5.0",
      "schema_version": "1.5.0",
      "content_versions": {
        "cards": {"version":"1.5.1","record_count":0},
        "sets": {"version":"1.5.1","record_count":0},
        "sealed_products": {"version":"1.5.1","record_count":0},
        "treatments": {"version":"1.5.1","record_count":0},
        "taxonomy": {"version":"1.5.1","record_count":0},
        "prices": {"version":"1.5.1"},
        "slabs": {"version":"0.0.0","record_count":0},
        "images": {"version":"1.5.1"}
      },
      "bundled_assets": { "keyrune": {"version":"3.19.0"} },
      "retained_full_versions": ["1.5.0"],
      "checksums": {"metadata/treatments.json":"sha256:abc"}
    }
    """;

    private const string LegacyShape = """
    {
      "package_type": "delta",
      "generated_at": "2026-05-13T19:14:57.1776670+00:00",
      "base_full_version": "1.4.0",
      "schema_version": "1.0.0",
      "content_versions": {
        "cards": {"version":"1.4.1","record_count":0},
        "sets": {"version":"1.4.1","record_count":0},
        "images": {"version":"1.4.1"},
        "slabs": {"version":"0.0.0","record_count":0},
        "keyrune": {"version":"3.18.0"}
      },
      "retained_full_versions": ["1.4.0"],
      "checksums": {}
    }
    """;

    [Fact]
    public void Current_Shape_Yields_The_Published_Version_And_Bundled_Font()
    {
        var manifest = JsonSerializer.Deserialize<PackageManifest>(CurrentShape, Options)!;

        Assert.Equal("1.5.1", manifest.Version);
        Assert.Equal("1.5.1", PackageVersion.For(manifest));
        Assert.Equal("3.19.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));

        // slabs stays pinned and is Product-owned; it must not influence anything.
        Assert.Equal("0.0.0", manifest.ContentVersions["slabs"].Version);
        Assert.DoesNotContain("keyrune", manifest.ContentVersions.Keys);
    }

    [Fact]
    public void Legacy_Shape_Still_Resolves_Without_A_Published_Version()
    {
        var manifest = JsonSerializer.Deserialize<PackageManifest>(LegacyShape, Options)!;

        Assert.Null(manifest.Version);
        Assert.Empty(manifest.BundledAssets);
        Assert.Equal("1.4.1", PackageVersion.For(manifest));
        // keyrune is still inside content_versions on this shape.
        Assert.Equal("3.18.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));
    }

    [Fact]
    public void A_Field_This_Build_Does_Not_Model_Does_Not_Fault_The_Parser()
    {
        // The Backend can add manifest fields without a Product release; a strict parser
        // would turn that into an outage on every deployment.
        const string withUnknowns = """
        {
          "version": "2.0.0",
          "package_type": "full",
          "schema_version": "1.5.0",
          "content_versions": { "cards": {"version":"2.0.0","record_count":5} },
          "bundled_assets": { "keyrune": {"version":"4.0.0"}, "some_future_font": {"version":"1.0.0"} },
          "an_entirely_new_top_level_field": { "nested": [1, 2, 3] },
          "checksums": {}
        }
        """;

        var manifest = JsonSerializer.Deserialize<PackageManifest>(withUnknowns, Options)!;

        Assert.Equal("2.0.0", PackageVersion.For(manifest));
        Assert.Equal("4.0.0", PackageVersion.BundledAssetVersion(manifest, "keyrune"));
        Assert.Equal("1.0.0", PackageVersion.BundledAssetVersion(manifest, "some_future_font"));
    }

    [Fact]
    public void The_Discovery_Index_Carries_A_Version_Per_Package_Entry()
    {
        // Lets a package be selected straight from the index without fetching its manifest.
        const string index = """
        {
          "version": "1.5.1",
          "schema_version": "1.5.0",
          "generated_at": "2026-09-07T05:51:26Z",
          "minimum_product_version": "1.0.0",
          "content_versions": { "cards": {"version":"1.5.1"} },
          "packages": [
            {
              "package_id": "20260907-055037-e557b8",
              "package_type": "delta",
              "version": "1.5.1",
              "download_url": "https://packages.countorsell.com/publish-a/x/package.zip",
              "manifest_url": "https://packages.countorsell.com/publish-a/x/manifest.json",
              "base_full_version": "1.5.0",
              "generated_at": "2026-09-07T05:51:26Z"
            },
            {
              "package_id": "older-entry-without-a-version",
              "package_type": "full",
              "download_url": "https://packages.countorsell.com/publish-b/y/package.zip",
              "manifest_url": "https://packages.countorsell.com/publish-b/y/manifest.json",
              "generated_at": "2026-05-13T19:14:57Z"
            }
          ]
        }
        """;

        var manifest = JsonSerializer.Deserialize<UpdateManifest>(index, Options)!;

        Assert.Equal("1.5.1", manifest.Version);
        Assert.Equal("1.5.1", manifest.Packages[0].Version);
        // An entry published before the field existed simply has none.
        Assert.Null(manifest.Packages[1].Version);
    }
}
