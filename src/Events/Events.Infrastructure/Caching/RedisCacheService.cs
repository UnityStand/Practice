using System.Text.Json;
using Events.Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Events.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis,ILogger<RedisCacheService> logger ): ICacheService
{
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try                                                                                                                                                                  
        {                                                                                                                                                                    
            var value = await redis.GetDatabase().StringGetAsync(key);                                                                                                      
            return 
                !value.HasValue ? null : 
                JsonSerializer.Deserialize<T>(value.ToString());
        }                                                                                                                                                                    
        catch (Exception ex ) when (ex is RedisConnectionException or RedisTimeoutException)                                                                                                                         
        {                                                                                                                                                                    
            logger.LogWarning(ex, "Redis is not available {key}", key);                                                                                                                              
            return null;                                                                                                     
        }    
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await redis.GetDatabase().StringSetAsync(key, json, ttl);   
        }
        catch (Exception ex ) when (ex is RedisConnectionException or RedisTimeoutException)         
        {
            logger.LogWarning(ex, "Redis is not available {key}", key);                                                                                                                              
        }
       
    }

    public async Task DeleteAsync(string key)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(key);  
        }
        catch (Exception ex ) when (ex is RedisConnectionException or RedisTimeoutException)         
        {
            logger.LogWarning(ex, "Redis is not available {key}", key);                                                                                                                              
        }
    }
}