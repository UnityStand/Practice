using Users.Domain.Entities;

namespace Users.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(string login);
    Task AddAsync(User user);

}