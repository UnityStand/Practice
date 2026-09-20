using EventApi.Domain.Entities;

namespace EventApi.Application.Services;

public interface IUserService
{
    Task RegisterAsync(string login, string password, UserRole role);                                    
    Task<string> LoginAsync(string login, string password);  
}