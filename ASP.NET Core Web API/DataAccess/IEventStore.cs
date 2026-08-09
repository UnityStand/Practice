using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public interface IEventStore
{
    IEnumerable<Event> GetAll();
    Event? Get(Guid id);
    Event Add(Event @event);
    void Remove(Event @event);
}