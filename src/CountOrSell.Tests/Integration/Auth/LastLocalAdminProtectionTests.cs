using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace CountOrSell.Tests.Integration.Auth;

/// <summary>
/// Tests that verify the last local admin cannot be removed, disabled, or demoted.
/// Each test creates its own isolated PostgreSQL container to avoid state accumulation.
/// </summary>
[Trait("Category", "RequiresDocker")]
public class LastLocalAdminBlockedTests
{
    private static User MakeLocalAdmin() => new()
    {
        Id = Guid.NewGuid(),
        Username = $"admin_{Guid.NewGuid():N}",
        DisplayName = "Admin",
        AuthType = AuthType.Local,
        Role = UserRole.Admin,
        IsBuiltinAdmin = false,
        State = AccountState.Active,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword123456"),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static async Task<(AppDbContext db, PostgreSqlContainer container)> CreateFreshDbAsync()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (db, container);
    }

    private static UserService BuildUserService(AppDbContext db)
    {
        var mockExport = new Mock<IExportService>();
        mockExport.Setup(e => e.ExportUserDataAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserExportFile { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, RemovedAt = DateTime.UtcNow });
        return new UserService(
            new UserRepository(db),
            mockExport.Object,
            new CollectionRepository(db),
            new SerializedRepository(db),
            new SlabRepository(db),
            new SealedInventoryRepository(db),
            new WishlistRepository(db),
            db);
    }

    [Fact]
    public async Task LastLocalAdmin_CannotBeRemoved_WhenOnlyOneExists()
    {
        var (db, container) = await CreateFreshDbAsync();
        await using var _ = db;
        await using var __ = container;

        var onlyAdmin = MakeLocalAdmin();
        db.Users.Add(onlyAdmin);
        await db.SaveChangesAsync();

        var service = BuildUserService(db);
        var result = await service.RemoveUserAsync(onlyAdmin.Id);

        Assert.False(result.Success);
        Assert.Contains("last local admin", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LastLocalAdmin_CannotBeDisabled_WhenOnlyOneExists()
    {
        var (db, container) = await CreateFreshDbAsync();
        await using var _ = db;
        await using var __ = container;

        var onlyAdmin = MakeLocalAdmin();
        db.Users.Add(onlyAdmin);
        await db.SaveChangesAsync();

        var service = BuildUserService(db);
        var result = await service.DisableUserAsync(onlyAdmin.Id);

        Assert.False(result.Success);
        Assert.Contains("last local admin", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LastLocalAdmin_CannotBeDemoted_WhenOnlyOneExists()
    {
        var (db, container) = await CreateFreshDbAsync();
        await using var _ = db;
        await using var __ = container;

        var onlyAdmin = MakeLocalAdmin();
        db.Users.Add(onlyAdmin);
        await db.SaveChangesAsync();

        var service = BuildUserService(db);
        var result = await service.DemoteUserAsync(onlyAdmin.Id);

        Assert.False(result.Success);
        Assert.Contains("last local admin", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Tests that verify operations succeed when sufficient local admins exist.
/// </summary>
[Trait("Category", "RequiresDocker")]
public class LastLocalAdminAllowedTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public LastLocalAdminAllowedTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static User MakeLocalAdmin(bool isBuiltin = false) => new()
    {
        Id = Guid.NewGuid(),
        Username = $"admin_{Guid.NewGuid():N}",
        DisplayName = "Admin",
        AuthType = AuthType.Local,
        Role = UserRole.Admin,
        IsBuiltinAdmin = isBuiltin,
        State = AccountState.Active,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword123456"),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static User MakeOAuthAdmin() => new()
    {
        Id = Guid.NewGuid(),
        Username = $"oauthadmin_{Guid.NewGuid():N}",
        DisplayName = "OAuth Admin",
        AuthType = AuthType.Google,
        Role = UserRole.Admin,
        IsBuiltinAdmin = false,
        State = AccountState.Active,
        OAuthProvider = "google",
        OAuthProviderUserId = Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UserService BuildUserService(AppDbContext db)
    {
        var mockExport = new Mock<IExportService>();
        mockExport.Setup(e => e.ExportUserDataAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserExportFile { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, RemovedAt = DateTime.UtcNow });
        return new UserService(
            new UserRepository(db),
            mockExport.Object,
            new CollectionRepository(db),
            new SerializedRepository(db),
            new SlabRepository(db),
            new SealedInventoryRepository(db),
            new WishlistRepository(db),
            db);
    }

    [Fact]
    public async Task OAuthAdmin_CanBeRemoved_WhenOneLocalAdminExists()
    {
        await using var db = _fixture.CreateContext();
        var localAdmin = MakeLocalAdmin();
        var oauthAdmin = MakeOAuthAdmin();
        db.Users.AddRange(localAdmin, oauthAdmin);
        await db.SaveChangesAsync();

        var service = BuildUserService(db);
        var result = await service.RemoveUserAsync(oauthAdmin.Id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task LocalAdmin_CanBeRemoved_WhenTwoLocalAdminsExist()
    {
        await using var db = _fixture.CreateContext();
        var admin1 = MakeLocalAdmin();
        var admin2 = MakeLocalAdmin();
        db.Users.AddRange(admin1, admin2);
        await db.SaveChangesAsync();

        var service = BuildUserService(db);
        var result = await service.RemoveUserAsync(admin1.Id);

        Assert.True(result.Success);
    }
}

// Empty placeholder so references to the old class name compile
public class LastLocalAdminProtectionTests
{
}
