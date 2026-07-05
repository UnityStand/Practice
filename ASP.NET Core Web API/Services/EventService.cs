using ASP.NET_Core_Web_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Services;

public class EventService:IEventService
{
    private static List<Event> Events { get; set; } = [];

    public List<Event>  GetEvents()
    {
        return Events;
    }

    public Event? GetEventById(int id)
    {
        //  FirstOrDefault вернёт null, если события с таким id нет — контроллер это использует для 404. 
        return Events.FirstOrDefault(e => e.Id == id);
    }

    public Event CreateEvent(Event newEvent)
    {   
        newEvent.Id = Events.Count == 0 ? 1 : Events.Max(e => e.Id) + 1;  
        Events.Add(newEvent);
        return newEvent;
    }

    public Event UpdateEvent(Event updatedEvent)
    {
        var existingEvent  = Events.FirstOrDefault(e => e.Id == updatedEvent.Id);
        if (existingEvent == null) return null; 
        
        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartDate=updatedEvent.StartDate;
        existingEvent.EndDate=updatedEvent.EndDate;
        
        return existingEvent;
    }

    public bool DeleteEvent(int id)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
        if (existingEvent == null) return false;
        Events.Remove(existingEvent);
        return true;
    }
}