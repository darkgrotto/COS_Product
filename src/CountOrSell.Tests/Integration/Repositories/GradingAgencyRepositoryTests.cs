using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Xunit;

namespace CountOrSell.Tests.Integration.Repositories;

public class GradingAgencyRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public GradingAgencyRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAll_ReturnsSeedData()
    {
        await using var db = _fixture.CreateContext();
        var repo = new GradingAgencyRepository(db);

        var agencies = await repo.GetAllAsync();

        Assert.True(agencies.Count >= 6);
    }

    [Fact]
    public async Task GetByCode_ReturnsCanonicalAgency()
    {
        await using var db = _fixture.CreateContext();
        var repo = new GradingAgencyRepository(db);

        var psa = await repo.GetByCodeAsync("psa");

        Assert.NotNull(psa);
        Assert.Equal("Professional Sports Authenticator", psa.FullName);
    }

    [Fact]
    public async Task CreateLocalAgency_Succeeds()
    {
        await using var db = _fixture.CreateContext();
        var repo = new GradingAgencyRepository(db);

        var local = new GradingAgency
        {
            Code = $"tst{Guid.NewGuid():N}"[..6],
            FullName = "Test Local Agency",
            ValidationUrlTemplate = "https://example.com/verify/{0}",
            SupportsDirectLookup = true,
            Source = AgencySource.Local,
            Active = true
        };

        await repo.CreateAsync(local);

        var found = await repo.GetByCodeAsync(local.Code);
        Assert.NotNull(found);
        Assert.Equal(AgencySource.Local, found.Source);
    }
}
