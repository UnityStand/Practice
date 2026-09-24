using Events.Application.Abstractions;
using Events.Infrastructure.Caching;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));        
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));       
        services.AddHostedService<KafkaTopicInitializer>(); 
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedBookingRepository, ProcessedBookingRepository>();
        services.AddHostedService<BookingConfirmedConsumer>();
        services.AddSingleton<IConnectionMultiplexer>(sp =>                                                                                
        {                                                                                                                                  
            var redis = sp.GetRequiredService<IOptions<RedisOptions>>().Value;                                                             
            var options = ConfigurationOptions.Parse(redis.ConnectionString);  
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);                                                                                 
        });      
        return services;
    }
}