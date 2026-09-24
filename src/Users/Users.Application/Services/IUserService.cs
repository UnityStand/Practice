using Users.Domain.Entities;

namespace Users.Application.Services;

public interface IUserService
{
    Task RegisterAsync(string login, string password, UserRole role);
    Task<string> LoginAsync(string login, string password);
}