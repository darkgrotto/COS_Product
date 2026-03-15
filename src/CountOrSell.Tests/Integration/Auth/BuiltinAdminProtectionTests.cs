using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CountOrSell.Tests.Integration.Auth;

public class BuiltinAdminProtectionTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public BuiltinAdminProtectionTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<User> CreateBuiltinAdmin(AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"builtinadmin_{Guid.NewGuid():N}",
            DisplayName = "Built-in Admin",
            AuthType = AuthType.Local,
            Role = UserRole.Admin,
            IsBuiltinAdmin = true,
            State = AccountState.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword123456"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task BuiltinAdmin_CannotBeDisabled()
    {
        await using var db = _fixture.CreateContext();
        var admin = await CreateBuiltinAdmin(db);
        var service = new UserService(new UserRepository(db));

        var result = await service.DisableUserAsync(admin.Id);

        Assert.False(result.Success);
        Assert.Contains("built-in admin", result.Error, StringComparison.OrdinalIgnoreCase);

        var unchanged = await db.Users.FindAsync(admin.Id);
        Assert.Equal(AccountState.Active, unchanged!.State);
    }

    [Fact]
    public async Task BuiltinAdmin_CannotBeRemoved()
    {
        await using var db = _fixture.CreateContext();
        var admin = await CreateBuiltinAdmin(db);
        var service = new UserService(new UserRepository(db));

        var result = await service.RemoveUserAsync(admin.Id);

        Assert.False(result.Success);
        Assert.Contains("built-in admin", result.Error, StringComparison.OrdinalIgnoreCase);

        var unchanged = await db.Users.FindAsync(admin.Id);
        Assert.NotEqual(AccountState.Removed, unchanged!.State);
    }

    [Fact]
    public async Task BuiltinAdmin_CannotBeDemoted()
    {
        await using var db = _fixture.CreateContext();
        var admin = await CreateBuiltinAdmin(db);
        var service = new UserService(new UserRepository(db));

        var result = await service.DemoteUserAsync(admin.Id);

        Assert.False(result.Success);
        Assert.Contains("built-in admin", result.Error, StringComparison.OrdinalIgnoreCase);

        var unchanged = await db.Users.FindAsync(admin.Id);
        Assert.Equal(UserRole.Admin, unchanged!.Role);
    }
}
