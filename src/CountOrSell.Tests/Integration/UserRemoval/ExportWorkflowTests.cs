using CountOrSell.Api.Auth;
using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.UserRemoval;

[Trait("Category", "RequiresDocker")]
public class ExportWorkflowTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public ExportWorkflowTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<User> SeedUserWithCollectionDataAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, Username = $"u_{Guid.NewGuid():N}", DisplayName = "Test User",
            AuthType = AuthType.Local, Role = UserRole.GeneralUser,
            IsBuiltinAdmin = false, State = AccountState.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123456"),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        if (!await db.Treatments.AnyAsync(t => t.Key == "regular"))
            db.Treatments.Add(new Treatment { Key = "regular", DisplayName = "Regular", SortOrder = 1 });
        if (!await db.Sets.AnyAsync(s => s.Code == "ex1"))
            db.Sets.Add(new Set { Code = "ex1", Name = "Export Test Set", TotalCards = 5, UpdatedAt = DateTime.UtcNow });
        if (!await db.Cards.AnyAsync(c => c.Identifier == "ex1001"))
            db.Cards.Add(new Card { Identifier = "ex1001", SetCode = "ex1", Name = "Export Card", CurrentMarketValue = 5m, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.CollectionEntries.Add(new CollectionEntry
        {
            Id = Guid.NewGuid(), UserId = userId, CardIdentifier = "ex1001",
            TreatmentKey = "regular", Quantity = 2, Condition = CardCondition.NM,
            Autographed = false, AcquisitionDate = DateOnly.FromDateTime(DateTime.Today),
            AcquisitionPrice = 4m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return user;
    }

    private static UserService BuildUserService(AppDbContext db, IExportService exportService)
    {
        return new UserService(
            new UserRepository(db),
            exportService,
            new CollectionRepository(db),
            new SerializedRepository(db),
            new SlabRepository(db),
            new SealedInventoryRepository(db),
            new WishlistRepository(db),
            new Mock<ILocalAuthService>().Object,
            db);
    }

    [Fact]
    public async Task RemoveUser_ExportFileCreated_BeforeDataDeleted()
    {
        await using var db = _fixture.CreateContext();
        var user = await SeedUserWithCollectionDataAsync(db);

        var exportDir = Path.Combine(Path.GetTempPath(), $"cos_export_test_{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExportDirectory"] = exportDir })
            .Build();

        var exportFileRepo = new UserExportFileRepository(db);
        var exportService = new ExportService(
            new CollectionRepository(db),
            new SerializedRepository(db),
            new SlabRepository(db),
            new SealedInventoryRepository(db),
            new WishlistRepository(db),
            new UserRepository(db),
            exportFileRepo,
            configuration);

        var userService = BuildUserService(db, exportService);
        var result = await userService.RemoveUserAsync(user.Id);

        Assert.True(result.Success);

        // Verify export file record was created
        await using var verifyDb = _fixture.CreateContext();
        var exportFiles = await verifyDb.UserExportFiles.Where(f => f.UserId == user.Id).ToListAsync();
        Assert.Single(exportFiles);
        Assert.Equal(user.Username, exportFiles[0].Username);

        // Verify collection data was deleted
        var remainingEntries = await verifyDb.CollectionEntries.Where(e => e.UserId == user.Id).ToListAsync();
        Assert.Empty(remainingEntries);

        // Verify user is marked as removed
        var removedUser = await verifyDb.Users.FindAsync(user.Id);
        Assert.NotNull(removedUser);
        Assert.Equal(AccountState.Removed, removedUser.State);

        // Cleanup
        if (Directory.Exists(exportDir))
            Directory.Delete(exportDir, true);
    }
}
