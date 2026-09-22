using EventApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}