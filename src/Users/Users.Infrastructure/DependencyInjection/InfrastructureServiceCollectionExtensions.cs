using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Abstractions;
using Users.Application.Options;
using Users.Application.Services;
using Users.Infrastructure.Persistence;
using Users.Infrastructure.Security;

namespace Users.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
       
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));    
 
        return services;
    }
}