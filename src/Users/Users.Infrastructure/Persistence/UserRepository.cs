using Users.Application.Abstractions;
using Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Users.Infrastructure.Persistence;

public class UserRepository(UsersDbContext context) : IUserRepository

{
    public async Task<User?> GetByLoginAsync(string login)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Login == login);
    }

    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
}