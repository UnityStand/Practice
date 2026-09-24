using Bookings.Application.Abstractions;
using Bookings.Application.DependencyInjection;
using Bookings.Application.Options;
using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Tests;

// DI как в Bookings.Api, но с InMemory-базой и фейковым издателем вместо Kafka.
// Каждый Create* берёт сервис из НОВОГО scope — свежий DbContext без кеша change tracker.
public sealed class TestServices : IDisposable
{
    public const int MaxActiveBookingsPerUser = 10;

    private readonly List<IServiceScope> _scopes = [];

    public TestServices()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<BookingDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.Configure<BookingSettings>(o => o.MaxActiveBookingsPerUser = MaxActiveBookingsPerUser);
        services.AddApplicationServices();

        Publisher = new RecordingPublisher();
        services.AddSingleton<IBookingEventPublisher>(Publisher);

        Provider = services.BuildServiceProvider();
        Publisher.ScopeFactory = Provider.GetRequiredService<IServiceScopeFactory>();
    }

    public ServiceProvider Provider { get; }

    public RecordingPublisher Publisher { get; }

    public T Create<T>() where T : notnull
    {
        var scope = Provider.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        Provider.Dispose();
    }
}
