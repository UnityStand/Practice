using EventApi.Application.Abstractions;
using EventApi.Application.Options;
using EventApi.Application.Services;
using Microsoft.Extensions.Options;
using EventApi.Infrastructure.Persistence;
using EventApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventApi.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,         
        IConfiguration configuration)                                                                                  {                                                                                                              services.AddDbContext<AppDbContext>(options =>                                                                 options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));                  
                                                                                                           
        services.AddScoped<IEventRepository, EventRepository>();                                         
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();                                                        
        services.AddScoped<IJwtTokenService, JwtTokenService>();   
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));       
        return services;                                                                                       }   
}