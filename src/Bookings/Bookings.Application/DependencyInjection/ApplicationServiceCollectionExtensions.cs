using Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingBackgroundService>();
        return services;
    }
}