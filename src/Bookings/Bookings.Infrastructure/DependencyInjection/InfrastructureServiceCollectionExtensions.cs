using Bookings.Application.Abstractions;
using Bookings.Infrastructure.Messaging;
using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BookingDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));                                                           
        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>(); 

        return services;
    }
}