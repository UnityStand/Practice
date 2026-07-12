using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class EventService:IEventService
{
    private  List<Event> Events { get; set; } = [];

    public PaginatedResult<Event>  GetEvents(string? title, DateTime? from, DateTime? to, int page =1 ,int pageSize = 10 )
    {
        var query = Events.AsEnumerable(); 
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.Contains(title,StringComparison.OrdinalIgnoreCase));
        if (from != null)
            query = query.Where(e => e.StartAt >= from);
        if (to != null)
            query = query.Where(e => e.EndAt <= to);
        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public Event GetEventById(int id)
    {
       var result = Events.FirstOrDefault(e => e.Id == id);
       return result ?? throw new NotFoundException($"Событие с id {id} не найдено");
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
        if (existingEvent == null) throw new NotFoundException($"Событие с id {updatedEvent.Id } не найдено"); 
        
        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartAt=updatedEvent.StartAt;
        existingEvent.EndAt=updatedEvent.EndAt;
        
        return existingEvent;
    }

    public bool DeleteEvent(int id)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
        if (existingEvent == null) throw new NotFoundException($"Событие с id {id} не найдено");
        Events.Remove(existingEvent);
        return true;
    }
}