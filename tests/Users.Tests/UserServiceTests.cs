using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Users.Application.Options;
using Users.Application.Services;
using Users.Domain.Entities;
using Users.Infrastructure.Persistence;
using Users.Infrastructure.Security;

namespace Users.Tests;

public class UserServiceTests : IDisposable
{
    private static readonly JwtOptions Jwt = new()
    {
        Secret = "test-secret-that-is-at-least-32-characters",
        Issuer = "User",
        Audience = "ApiClient",
        ExpiryMinutes = 60
    };

    private readonly UsersDbContext _context;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new UsersDbContext(options);
        _service = new UserService(
            new UserRepository(_context),
            new PasswordHasher(),
            new JwtTokenService(Options.Create(Jwt)));
    }

    public void Dispose() => _context.Dispose();

    // Те же параметры проверки, что в Program.cs сервисов Events и Bookings
    private static ClaimsPrincipal ValidateLikeOtherServices(string token, string secret)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = Jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
        return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
    }

    [Fact]
    public async Task RegisterThenLogin_ReturnsToken()
    {
        await _service.RegisterAsync("alice", "pass", UserRole.Customer);

        var token = await _service.LoginAsync("alice", "pass");

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Register_Throws_WhenLoginAlreadyTaken()
    {
        await _service.RegisterAsync("alice", "pass", UserRole.Customer);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RegisterAsync("alice", "other", UserRole.Customer));
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        await _service.RegisterAsync("alice", "pass", UserRole.Customer);

        var user = await _context.Users.SingleAsync(u => u.Login == "alice");
        Assert.NotEqual("pass", user.HashedPassword);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordIsWrong()
    {
        await _service.RegisterAsync("alice", "pass", UserRole.Customer);

        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync("alice", "wrong"));
    }

    [Fact]
    public async Task Login_Throws_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync("nobody", "pass"));
    }

    [Fact]
    public async Task Token_IsAcceptedByOtherServices_AndCarriesUserIdAndRole()
    {
        await _service.RegisterAsync("admin", "pass", UserRole.Admin);
        var userId = (await _context.Users.SingleAsync(u => u.Login == "admin")).Id;

        var token = await _service.LoginAsync("admin", "pass");
        var principal = ValidateLikeOtherServices(token, Jwt.Secret);

        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.True(principal.IsInRole("Admin"));
    }

    [Fact]
    public async Task Token_IsRejected_WhenOtherServiceUsesDifferentSecret()
    {
        await _service.RegisterAsync("alice", "pass", UserRole.Customer);
        var token = await _service.LoginAsync("alice", "pass");

        Assert.ThrowsAny<SecurityTokenException>(
            () => ValidateLikeOtherServices(token, "another-secret-that-is-at-least-32-chars"));
    }
}
