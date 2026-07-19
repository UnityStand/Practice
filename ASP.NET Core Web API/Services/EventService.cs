using ASP.NET_Core_Web_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Services;

public class EventService:IEventService
{
    private  List<Event> Events { get; set; } = [];

    public List<Event>  GetEvents()
    {
        return Events;
    }

    public Event? GetEventById(Guid id)
    {
        //  FirstOrDefault вернёт null, если события с таким id нет — контроллер это использует для 404. 
        return Events.FirstOrDefault(e => e.Id == id);
    }

    public Event CreateEvent(Event newEvent)
    {   
        newEvent.Id = Guid.NewGuid();
        Events.Add(newEvent);
        return newEvent;
    }

    public Event? UpdateEvent(Event updatedEvent)
    {
        var existingEvent  = Events.FirstOrDefault(e => e.Id == updatedEvent.Id);
        if (existingEvent == null) return null; 
        
        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartAt=updatedEvent.StartAt;
        existingEvent.EndAt=updatedEvent.EndAt;
        
        return existingEvent;
    }

    public bool DeleteEvent(Guid id)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
        if (existingEvent == null) return false;
        Events.Remove(existingEvent);
        return true;
    }
}