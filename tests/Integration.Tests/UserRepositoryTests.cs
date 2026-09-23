using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Infrastructure.Persistence;

namespace Integration.Tests;

public class UserRepositoryTests(PostgresContainerFixture fixture)
    : DatabaseTestBase<UsersDbContext>(fixture, "user_db_test")
{
    protected override UsersDbContext CreateContext(DbContextOptions<UsersDbContext> options) => new(options);

    private UserRepository Repository => new(Context);

    [Fact]
    public async Task Migration_CreatesOnlyUsersTables()
    {
        var tables = await PublicTablesAsync();

        Assert.Equal(["Users", "__EFMigrationsHistory"], tables);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsUser_WhenExists()
    {
        var user = User.Create("existing-user", "hash", UserRole.Customer);
        await Repository.AddAsync(user);

        var result = await new UserRepository(NewContext()).GetByLoginAsync("existing-user");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(UserRole.Customer, result.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsNull_WhenNotExists()
    {
        Assert.Null(await Repository.GetByLoginAsync("no-such-user"));
    }

    [Fact]
    public async Task AddAsync_PersistsRoleAsString()
    {
        await Repository.AddAsync(User.Create("admin", "hash", UserRole.Admin));

        var saved = await NewContext().Users.SingleAsync(u => u.Login == "admin");
        Assert.Equal(UserRole.Admin, saved.Role);
    }

    [Fact]
    public async Task AddAsync_Throws_WhenLoginDuplicated()
    {
        await Repository.AddAsync(User.Create("duplicate", "hash1", UserRole.Customer));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => new UserRepository(NewContext()).AddAsync(User.Create("duplicate", "hash2", UserRole.Customer)));
    }
}
