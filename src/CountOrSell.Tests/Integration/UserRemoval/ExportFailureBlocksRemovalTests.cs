using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.UserRemoval;

[Trait("Category", "RequiresDocker")]
public class ExportFailureBlocksRemovalTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public ExportFailureBlocksRemovalTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RemoveUser_WhenExportFails_DataRemainsIntact()
    {
        await using var db = _fixture.CreateContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "Test User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false, State = AccountState.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123456"),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        if (!await db.Treatments.AnyAsync(t => t.Key == "regular"))
            db.Treatments.Add(new Treatment { Key = "regular", DisplayName = "Regular", SortOrder = 1 });
        if (!await db.Sets.AnyAsync(s => s.Code == "ef1"))
            db.Sets.Add(new Set { Code = "ef1", Name = "Fail Test Set", TotalCards = 5, UpdatedAt = DateTime.UtcNow });
        if (!await db.Cards.AnyAsync(c => c.Identifier == "ef1001"))
            db.Cards.Add(new Card { Identifier = "ef1001", SetCode = "ef1", Name = "Fail Card", CurrentMarketValue = 5m, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "ef1001",
            TreatmentKey = "regular", Quantity = 1, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 4m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Mock the export service to throw
        var mockExport = new Mock<IExportService>();
        mockExport.Setup(e => e.ExportUserDataAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Simulated disk failure"));

        var userService = new UserService(
            new UserRepository(db),
            mockExport.Object,
            new CollectionRepository(db),
            new SerializedRepository(db),
            new SlabRepository(db),
            new SealedInventoryRepository(db),
            new WishlistRepository(db),
            db);

        var result = await userService.RemoveUserAsync(userId);

        // Removal should have failed
        Assert.False(result.Success);
        Assert.Contains("Export failed", result.Error, StringComparison.OrdinalIgnoreCase);

        // Verify collection data still exists
        await using var verifyDb = _fixture.CreateContext();
        var entries = await verifyDb.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();
        Assert.NotEmpty(entries);

        // Verify user still exists and is not marked as removed
        var user = await verifyDb.Users.FindAsync(userId);
        Assert.NotNull(user);
        Assert.NotEqual(AccountState.Removed, user.State);
    }
}
