using System.Collections.Concurrent;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, Event> _events = [];

    public IEnumerable<Event> GetAll() => _events.Values;
    public Event? Get(Guid id) => _events.GetValueOrDefault(id);

    public Event Add(Event @event)
    {
        @event.Id = Guid.NewGuid();
        _events[@event.Id] = @event;
        return @event;

    }

    public void Remove(Event @event)
    {
        _events.TryRemove(@event.Id, out _);
    }
}
