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

    [Fact]
    public async Task CCC_HasSupportsDirectLookupFalse()
    {
        await using var db = _fixture.CreateContext();
        var ccc = await db.GradingAgencies.FindAsync("ccc");
        Assert.NotNull(ccc);
        Assert.False(ccc.SupportsDirectLookup);
    }

    [Theory]
    [InlineData("bgs")]
    [InlineData("psa")]
    [InlineData("sgc")]
    [InlineData("cgc")]
    [InlineData("isa")]
    public async Task CanonicalAgency_ExceptCCC_SupportsDirectLookup(string code)
    {
        await using var db = _fixture.CreateContext();
        var agency = await db.GradingAgencies.FindAsync(code);
        Assert.NotNull(agency);
        Assert.True(agency.SupportsDirectLookup);
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
