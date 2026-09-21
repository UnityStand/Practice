using EventApi.Domain.Entities;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

[Collection("Database")]
public class UserRepositoryTests : RepositoryTestBase
{
    public UserRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
    }

    private static User CreateTestUser(string login = "user1", string passwordHash = "hash", UserRole role = UserRole.Customer)
    {
        return User.Create(login, passwordHash, role);
    }

    [Fact]
    public async Task GetByLoginAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser(login: "existing-user");
        await UserRepository.AddAsync(user);

        // Act
        var result = await UserRepository.GetByLoginAsync("existing-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal(user.Login, result.Login);
        Assert.Equal(user.Role, result.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await UserRepository.GetByLoginAsync("no-such-user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        // Arrange
        var user = CreateTestUser(login: "persisted-user", role: UserRole.Admin);

        // Act
        await UserRepository.AddAsync(user);

        // Assert
        var saved = await Context.Users.FindAsync(user.Id);
        Assert.NotNull(saved);
        Assert.Equal("persisted-user", saved!.Login);
        Assert.Equal(UserRole.Admin, saved.Role);
    }

    [Fact]
    public async Task AddAsync_DuplicateLogin_ThrowsDueToUniqueConstraint()
    {
        // Arrange
        await UserRepository.AddAsync(CreateTestUser(login: "duplicate-login", passwordHash: "hash1"));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => UserRepository.AddAsync(CreateTestUser(login: "duplicate-login", passwordHash: "hash2")));
    }
}
