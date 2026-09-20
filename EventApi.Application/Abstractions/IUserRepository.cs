using EventApi.Domain.Entities;

namespace EventApi.Application.Abstractions;

public interface IUserRepository
{
   Task<User?> GetByLoginAsync(string login);
   Task AddAsync(User user);
   
}