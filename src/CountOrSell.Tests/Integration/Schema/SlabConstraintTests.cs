using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Schema;

[Trait("Category", "RequiresDocker")]
public class SlabConstraintTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public SlabConstraintTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid userId, string treatmentKey, string agencyCode)> SeedAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Username = $"slabuser_{Guid.NewGuid():N}",
            DisplayName = "Test",
            AuthType = AuthType.Local,
            Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false,
            State = AccountState.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword12345"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        const string treatmentKey = "regular";
        if (!await db.Treatments.AnyAsync(t => t.Key == treatmentKey))
        {
            db.Treatments.Add(new Treatment { Key = treatmentKey, DisplayName = "Regular", SortOrder = 1 });
        }

        await db.SaveChangesAsync();
        return (userId, treatmentKey, "psa"); // psa is seeded
    }

    [Fact]
    public async Task Slab_WithSerialNumberButNoPrintRunTotal_IsRejectedAtDatabaseLevel()
    {
        await using var db = _fixture.CreateContext();
        var (userId, treatmentKey, agencyCode) = await SeedAsync(db);

        var slab = new SlabEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = "eoe019",
            TreatmentKey = treatmentKey,
            GradingAgencyCode = agencyCode,
            Grade = "9",
            CertificateNumber = "12345678",
            SerialNumber = 42,
            PrintRunTotal = null, // violation: serial present but print run absent
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 100m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.SlabEntries.Add(slab);
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Slab_WithSerialNumberAndPrintRunTotal_IsAccepted()
    {
        await using var db = _fixture.CreateContext();
        var (userId, treatmentKey, agencyCode) = await SeedAsync(db);

        var slab = new SlabEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = "eoe019",
            TreatmentKey = treatmentKey,
            GradingAgencyCode = agencyCode,
            Grade = "9",
            CertificateNumber = $"cert_{Guid.NewGuid():N}",
            SerialNumber = 42,
            PrintRunTotal = 250,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 100m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.SlabEntries.Add(slab);
        await db.SaveChangesAsync();
        Assert.True(await db.SlabEntries.AnyAsync(s => s.Id == slab.Id));
    }

    [Fact]
    public async Task Slab_WithNeitherSerialNorPrintRun_IsAccepted()
    {
        await using var db = _fixture.CreateContext();
        var (userId, treatmentKey, agencyCode) = await SeedAsync(db);

        var slab = new SlabEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = "eoe019",
            TreatmentKey = treatmentKey,
            GradingAgencyCode = agencyCode,
            Grade = "10",
            CertificateNumber = $"cert_{Guid.NewGuid():N}",
            SerialNumber = null,
            PrintRunTotal = null,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 200m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.SlabEntries.Add(slab);
        await db.SaveChangesAsync();
        Assert.True(await db.SlabEntries.AnyAsync(s => s.Id == slab.Id));
    }
}
