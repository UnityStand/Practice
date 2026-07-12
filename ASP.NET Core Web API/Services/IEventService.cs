using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public interface IEventService
{
    PaginatedResult<Event>  GetEvents(string? title, DateTime? from, DateTime? to, int page =1 , int pageSize = 10);
    Event GetEventById(int id);
    Event CreateEvent(Event newEvent);
    Event UpdateEvent(Event updatedEvent);
    bool DeleteEvent(int id);
}