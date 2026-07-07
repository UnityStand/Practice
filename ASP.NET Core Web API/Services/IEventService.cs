using ASP.NET_Core_Web_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Services;

public interface IEventService
{
    List<Event>  GetEvents();
    Event? GetEventById(int id);
    Event CreateEvent(Event newEvent);
    Event? UpdateEvent(Event updatedEvent);
    bool DeleteEvent(int id);
}