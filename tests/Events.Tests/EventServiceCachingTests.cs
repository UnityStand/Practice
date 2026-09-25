using Events.Application.Caching;
using Events.Application.DTOs;
using Events.Application.Services;
using Events.Domain.Entities;
using Events.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace Events.Tests;

// Unit-тесты Cache-Aside: EventService с заглушками вместо Redis и базы.
public class EventServiceCachingTests
{
    private static readonly CacheOptions CacheOptions = new()
    {
        EventTtl = TimeSpan.FromMinutes(5),
        TopEventsTtl = TimeSpan.FromMinutes(1)
    };

    private readonly FakeCacheService _cache = new();

    private EventService Service(StubEventRepository repository) =>
        new(repository, new StubProcessedBookingRepository(), _cache, Options.Create(CacheOptions));

    private static Event NewEvent(string title = "Concert", int totalSeats = 10, int soldSeats = 0)
    {
        var ev = Event.Create(title, null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), totalSeats);
        if (soldSeats > 0)
            ev.TryReserveSeats(soldSeats);
        return ev;
    }

    // GET /events/{id}

    [Fact]
    public async Task GetEventById_CacheHit_ReturnsCachedValue_WithoutRepository()
    {
        var ev = NewEvent();
        var repository = new StubEventRepository(ev);
        var cached = new EventResponseDto { EventId = ev.Id, Title = "From cache" };
        await _cache.SetAsync(CacheKeys.Event(ev.Id), cached, CacheOptions.EventTtl);

        var result = await Service(repository).GetEventById(ev.Id);

        Assert.Same(cached, result);
        Assert.Equal(0, repository.GetByIdCalls);
    }

    [Fact]
    public async Task GetEventById_CacheMiss_ReadsRepository_AndCachesWithEventTtl()
    {
        var ev = NewEvent(title: "From DB");
        var repository = new StubEventRepository(ev);
        var key = CacheKeys.Event(ev.Id);

        var result = await Service(repository).GetEventById(ev.Id);

        Assert.Equal("From DB", result.Title);
        Assert.Equal(1, repository.GetByIdCalls);
        Assert.Same(result, _cache.Peek<EventResponseDto>(key));
        Assert.Equal(CacheOptions.EventTtl, _cache.TtlOf(key));
    }

    [Fact]
    public async Task GetEventById_SecondCall_HitsCache()
    {
        var ev = NewEvent();
        var repository = new StubEventRepository(ev);
        var service = Service(repository);

        await service.GetEventById(ev.Id);
        await service.GetEventById(ev.Id);

        Assert.Equal(1, repository.GetByIdCalls);
    }

    // GET /events/top

    [Fact]
    public async Task GetTopEvents_CacheHit_ReturnsCachedValue_WithoutRepository()
    {
        var repository = new StubEventRepository(NewEvent());
        List<EventResponseDto> cached = [new() { Title = "From cache" }];
        await _cache.SetAsync(CacheKeys.TopEvents, cached, CacheOptions.TopEventsTtl);

        var result = await Service(repository).GetTopEvents();

        Assert.Same(cached, result);
        Assert.Equal(0, repository.GetTopCalls);
    }

    [Fact]
    public async Task GetTopEvents_CacheMiss_ReadsRepository_AndCachesWithTopEventsTtl()
    {
        var repository = new StubEventRepository(
            NewEvent("Half sold", soldSeats: 5),
            NewEvent("Sold out", soldSeats: 10),
            NewEvent("Empty"));

        var result = await Service(repository).GetTopEvents();

        Assert.Equal(["Sold out", "Half sold", "Empty"], result.Select(e => e.Title));
        Assert.Equal(1, repository.GetTopCalls);
        Assert.Same(result, _cache.Peek<List<EventResponseDto>>(CacheKeys.TopEvents));
        Assert.Equal(CacheOptions.TopEventsTtl, _cache.TtlOf(CacheKeys.TopEvents));
    }

    // Инвалидация при записи

    [Fact]
    public async Task UpdateEvent_InvalidatesEventKey_AndNextReadReturnsFreshData()
    {
        var ev = NewEvent(title: "Old title");
        var service = Service(new StubEventRepository(ev));
        await service.GetEventById(ev.Id); // прогреваем кеш

        await service.UpdateEvent(ev.Id, "New title", null, ev.StartAt, ev.EndAt);

        Assert.False(_cache.Contains(CacheKeys.Event(ev.Id)));
        Assert.Equal("New title", (await service.GetEventById(ev.Id)).Title);
    }

    [Fact]
    public async Task DeleteEvent_InvalidatesEventKey()
    {
        var ev = NewEvent();
        var service = Service(new StubEventRepository(ev));
        await service.GetEventById(ev.Id);

        await service.DeleteEvent(ev.Id);

        Assert.False(_cache.Contains(CacheKeys.Event(ev.Id)));
    }

    [Fact]
    public async Task UpdateEvent_KeepsTopEventsKey()
    {
        var ev = NewEvent();
        var service = Service(new StubEventRepository(ev));
        await service.GetTopEvents();

        await service.UpdateEvent(ev.Id, "New title", null, ev.StartAt, ev.EndAt);

        Assert.True(_cache.Contains(CacheKeys.TopEvents)); // топ живёт по TTL
    }
}
