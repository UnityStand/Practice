using Events.Application.Abstractions;
using Events.Application.Messaging;
using Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingConfirmedHandler, BookingConfirmedHandler>();

        return services;
    }
}