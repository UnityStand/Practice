using Events.Application.Abstractions;
using Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
       

        services.AddScoped<IEventRepository, EventRepository>();

 
        return services;
    }
}