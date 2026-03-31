using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Schema;

[Trait("Category", "RequiresDocker")]
public class CardIdentifierConstraintTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public CardIdentifierConstraintTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<(AppDbContext db, Guid userId, string treatmentKey)> SeedPrerequisitesAsync(
        PostgreSqlFixture fixture)
    {
        var db = fixture.CreateContext();
        var userId = Guid.NewGuid();

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Username = $"testuser_{Guid.NewGuid():N}",
                DisplayName = "Test",
                AuthType = AuthType.Local,
                Role = UserRole.GeneralUser,
                IsBuiltinAdmin = false,
                State = AccountState.Active,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword12345"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        const string treatmentKey = "regular";
        if (!await db.Treatments.AnyAsync(t => t.Key == treatmentKey))
        {
            db.Treatments.Add(new Treatment
            {
                Key = treatmentKey,
                DisplayName = "Regular",
                SortOrder = 1
            });
        }

        await db.SaveChangesAsync();
        return (db, userId, treatmentKey);
    }

    [Fact]
    public async Task CardIdentifier_FourDigitSuffixBelow1000_IsRejected()
    {
        var (db, userId, treatmentKey) = await SeedPrerequisitesAsync(_fixture);
        await using var _ = db;

        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = "eoe0001", // four-digit suffix "0001" < 1000 - invalid
            TreatmentKey = treatmentKey,
            Quantity = 1,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CollectionEntries.Add(entry);
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CardIdentifier_FourDigitSuffixExactly1000_IsAccepted()
    {
        var (db, userId, treatmentKey) = await SeedPrerequisitesAsync(_fixture);
        await using var _ = db;

        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = "eoe1000", // four-digit suffix "1000" >= 1000 - valid
            TreatmentKey = treatmentKey,
            Quantity = 1,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CollectionEntries.Add(entry);
        await db.SaveChangesAsync(); // should succeed
        Assert.True(await db.CollectionEntries.AnyAsync(e => e.CardIdentifier == "eoe1000"));
    }

    [Theory]
    [InlineData("eoe019")]    // 3-digit suffix
    [InlineData("eoe999")]    // 3-digit suffix max
    [InlineData("eoe1234")]   // 4-digit suffix > 1000
    [InlineData("3ed019")]    // numeric char in set code
    [InlineData("pala001a")]  // trailing letter variant a
    [InlineData("pala001b")]  // trailing letter variant b - distinct card
    [InlineData("eoe1234a")]  // 4-digit suffix with trailing letter
    [InlineData("drk077x")]   // dagger (†) variant - synthetic "x" suffix
    [InlineData("arn002x")]   // ARN light/dark tap symbol variant
    public async Task CardIdentifier_ValidFormats_AreAccepted(string identifier)
    {
        var (db, userId, treatmentKey) = await SeedPrerequisitesAsync(_fixture);
        await using var _ = db;

        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardIdentifier = identifier,
            TreatmentKey = treatmentKey,
            Quantity = 1,
            Condition = CardCondition.NM,
            Autographed = false,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 1.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CollectionEntries.Add(entry);
        await db.SaveChangesAsync();
        Assert.True(await db.CollectionEntries.AnyAsync(e => e.CardIdentifier == identifier));
    }
}
