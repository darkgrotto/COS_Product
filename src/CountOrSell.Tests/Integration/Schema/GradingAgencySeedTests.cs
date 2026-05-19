using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Schema;

[Trait("Category", "RequiresDocker")]
public class GradingAgencySeedTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public GradingAgencySeedTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("bgs")]
    [InlineData("psa")]
    [InlineData("sgc")]
    [InlineData("cgc")]
    [InlineData("ccc")]
    [InlineData("isa")]
    public async Task CanonicalAgency_IsSeeded(string code)
    {
        await using var db = _fixture.CreateContext();
        var agency = await db.GradingAgencies.FindAsync(code);
        Assert.NotNull(agency);
        Assert.Equal(AgencySource.Canonical, agency.Source);
        Assert.True(agency.Active);
    }

    // Agencies that publish a documented direct cert-lookup URL.
    [Theory]
    [InlineData("psa")]
    [InlineData("cgc")]
    [InlineData("isa")]
    public async Task DirectLookupAgency_HasCertPlaceholderInUrl(string code)
    {
        await using var db = _fixture.CreateContext();
        var agency = await db.GradingAgencies.FindAsync(code);
        Assert.NotNull(agency);
        Assert.True(agency.SupportsDirectLookup);
        Assert.Contains("{cert}", agency.ValidationUrlTemplate);
    }

    // Agencies whose lookup is a JS-rendered form on a landing page with no
    // documented deep-link URL pattern. The cert number is displayed alongside
    // the link for manual entry in the UI.
    [Theory]
    [InlineData("bgs")]
    [InlineData("sgc")]
    [InlineData("ccc")]
    public async Task LandingOnlyAgency_HasNoCertPlaceholder(string code)
    {
        await using var db = _fixture.CreateContext();
        var agency = await db.GradingAgencies.FindAsync(code);
        Assert.NotNull(agency);
        Assert.False(agency.SupportsDirectLookup);
        Assert.DoesNotContain("{cert}", agency.ValidationUrlTemplate);
    }

    [Fact]
    public async Task AllSixCanonicalAgencies_AreSeeded()
    {
        await using var db = _fixture.CreateContext();
        var count = await db.GradingAgencies
            .Where(a => a.Source == AgencySource.Canonical)
            .CountAsync();
        Assert.Equal(6, count);
    }
}
