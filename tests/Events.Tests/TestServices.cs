using Events.Application.DependencyInjection;
using Events.Application.Abstractions;
using Events.Application.Caching;
using Events.Tests.Fakes;
using Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Tests;

// DI как в Events.Api, но с InMemory-базой вместо PostgreSQL.
// Каждый Create* берёт сервис из НОВОГО scope — свежий DbContext без кеша change tracker,
// так тесты видят реальное состояние базы, а не закешированные сущности.
public sealed class TestServices : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly List<IServiceScope> _scopes = [];

    // Singleton, как RedisCacheService: тест видит, что осталось в кеше после операций
    public FakeCacheService Cache { get; } = new();

    public TestServices()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<EventDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedBookingRepository, ProcessedBookingRepository>();
        services.AddSingleton<ICacheService>(Cache);
        services.Configure<CacheOptions>(o =>
        {
            o.EventTtl = TimeSpan.FromMinutes(5);
            o.TopEventsTtl = TimeSpan.FromMinutes(1);
        });
        services.AddApplicationServices();
        _provider = services.BuildServiceProvider();
    }

    public T Create<T>() where T : notnull
    {
        var scope = _provider.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _provider.Dispose();
    }
}
