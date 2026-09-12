using EventApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventApi.Application.DependencyInjection;                                                                                                                         
                                                                                                                                                                              
public static class ApplicationServiceCollectionExtensions                                                                                                                  
{                                                                                                                                                                           
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)                                                                               
    {                                                                                                                                                                       
        services.AddScoped<IEventService, EventService>();                                                                                                                  
        services.AddScoped<IBookingService, BookingService>();                                                                                                              
        services.AddHostedService<BookingBackgroundService>();                                                                                                              
        return services;                                                                                                                                                    
    }                                                                                                                                                                       
}