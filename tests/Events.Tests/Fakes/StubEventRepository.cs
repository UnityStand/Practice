using Events.Application.Abstractions;
using Events.Domain.Entities;

namespace Events.Tests.Fakes;

// Репозиторий на списке в памяти со счётчиками чтений: по ним видно, ходил ли сервис в базу.
public sealed class StubEventRepository(params Event[] events) : IEventRepository
{
    private readonly List<Event> _events = [.. events];

    public int GetByIdCalls { get; private set; }
    public int GetTopCalls { get; private set; }

    public Task<Event?> GetEventByIdAsync(Guid id)
    {
        GetByIdCalls++;
        return Task.FromResult(_events.FirstOrDefault(e => e.Id == id));
    }

    public Task<List<Event>> GetTopAsync(int count)
    {
        GetTopCalls++;
        return Task.FromResult(_events
            .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .Take(count)
            .ToList());
    }

    public Task<(List<Event> Items, int TotalCount)> GetPagedAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize) =>
        throw new NotSupportedException();

    public Task AddAsync(Event @event)
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Event @event) => Task.CompletedTask;

    public Task RemoveAsync(Event @event)
    {
        _events.Remove(@event);
        return Task.CompletedTask;
    }
}
