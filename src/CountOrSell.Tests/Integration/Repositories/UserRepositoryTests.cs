using CountOrSell.Data.Repositories;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Tests.Infrastructure;
using Xunit;

namespace CountOrSell.Tests.Integration.Repositories;

[Trait("Category", "RequiresDocker")]
public class UserRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public UserRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static User NewLocalUser(UserRole role = UserRole.GeneralUser) => new()
    {
        Id = Guid.NewGuid(),
        Username = $"user_{Guid.NewGuid():N}",
        DisplayName = "Test User",
        AuthType = AuthType.Local,
        Role = role,
        IsBuiltinAdmin = false,
        State = AccountState.Active,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword12345"),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAndGetById_Works()
    {
        await using var db = _fixture.CreateContext();
        var repo = new UserRepository(db);
        var user = NewLocalUser();

        var created = await repo.CreateAsync(user);
        var found = await repo.GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(user.Username, found.Username);
    }

    [Fact]
    public async Task GetByUsername_ReturnsUser()
    {
        await using var db = _fixture.CreateContext();
        var repo = new UserRepository(db);
        var user = NewLocalUser();
        await repo.CreateAsync(user);

        var found = await repo.GetByUsernameAsync(user.Username);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
    }

    [Fact]
    public async Task GetByUsername_ReturnsNull_WhenNotFound()
    {
        await using var db = _fixture.CreateContext();
        var repo = new UserRepository(db);

        var found = await repo.GetByUsernameAsync("nonexistent_user_xyz");

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateUser_PersistsChanges()
    {
        await using var db = _fixture.CreateContext();
        var repo = new UserRepository(db);
        var user = NewLocalUser();
        await repo.CreateAsync(user);

        user.DisplayName = "Updated Name";
        user.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(user);

        var found = await repo.GetByIdAsync(user.Id);
        Assert.Equal("Updated Name", found!.DisplayName);
    }

    [Fact]
    public async Task CountLocalAdmins_CountsCorrectly()
    {
        await using var db = _fixture.CreateContext();
        var repo = new UserRepository(db);

        var before = await repo.CountLocalAdminsAsync();

        var admin1 = NewLocalUser(UserRole.Admin);
        var admin2 = NewLocalUser(UserRole.Admin);
        var generalUser = NewLocalUser(UserRole.GeneralUser);
        await repo.CreateAsync(admin1);
        await repo.CreateAsync(admin2);
        await repo.CreateAsync(generalUser);

        var after = await repo.CountLocalAdminsAsync();
        Assert.Equal(before + 2, after);
    }
}
