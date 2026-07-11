using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public interface IEventService
{
    List<Event>  GetEvents(string? title, DateTime? from, DateTime? to);
    Event GetEventById(int id);
    Event CreateEvent(Event newEvent);
    Event UpdateEvent(Event updatedEvent);
    bool DeleteEvent(int id);
}