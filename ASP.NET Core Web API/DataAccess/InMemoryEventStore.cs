using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public class InMemoryEventStore:IEventStore
{
    private List<Event> Events { get; set; } = [];
    
    public IEnumerable<Event> GetAll()
    {
        return Events;
    }

    public Event? Get(Guid id)
    {
        return Events.FirstOrDefault(e => e.Id == id);
    }

    public Event Add(Event @event)
    {
        @event.Id = Guid.NewGuid();
        Events.Add(@event);
        return @event;
      
    }

    public void Remove(Event @event)
    {
        Events.Remove(@event); 
    }
}