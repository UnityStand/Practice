using EventApi.Application.Abstractions;
using EventApi.Application.Services;
using EventApi.Infrastructure.Persistence;
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
        return services;                                                                                       }   
}