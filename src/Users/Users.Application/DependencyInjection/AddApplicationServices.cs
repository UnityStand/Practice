using Microsoft.Extensions.DependencyInjection;
using Users.Application.Services;

namespace Users.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddScoped<IUserService, UserService>();

        return services;
    }
}