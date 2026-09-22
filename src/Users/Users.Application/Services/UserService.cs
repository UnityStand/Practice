using System.ComponentModel.DataAnnotations;
using Users.Application.Abstractions;
using Users.Domain.Entities;

namespace Users.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IUserService
{
    public async Task RegisterAsync(string login, string password, UserRole role)
    {
        var existing = await userRepository.GetByLoginAsync(login);
        if (existing != null)
            throw new ValidationException("Login already taken");
        var hashedPassword = passwordHasher.Hash(password);
        var user = User.Create(login, hashedPassword, role);
        await userRepository.AddAsync(user);


    }

    public async Task<string> LoginAsync(string login, string password)
    {
        var user = await userRepository.GetByLoginAsync(login);
        if (user == null || !passwordHasher.Verify(password, user.HashedPassword))
            throw new ValidationException("Invalid login or password");
        return jwtTokenService.GenerateToken(user.Id, user.Login, user.Role);
        ;
    }
}