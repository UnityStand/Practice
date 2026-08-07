using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class EventService(IEventStore eventStore) : IEventService
{

    private Event FindEventOrThrow(Guid id)
    {
        var result = eventStore.Get(id);
        if (result == null) throw new NotFoundException($"Event with id {id} not found");

        return result;
    }

    public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var query = eventStore.GetAll();
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (from != null)
            query = query.Where(e => e.StartAt >= from);
        if (to != null)
            query = query.Where(e => e.EndAt <= to);
        var total = query.Count();
        var items = query.OrderBy(e => e.StartAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public Event GetEventById(Guid id)
    {
        return FindEventOrThrow(id);
    }


    public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var newEvent = Event.Create(title, description, startAt, endAt, totalSeats);

        return eventStore.Add(newEvent);
    }

    public Event UpdateEvent(Event updatedEvent)
    {
        var existingEvent = FindEventOrThrow(updatedEvent.Id);

        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartAt = updatedEvent.StartAt;
        existingEvent.EndAt = updatedEvent.EndAt;

        return existingEvent;
    }

    public bool DeleteEvent(Guid id)
    {
        var existingEvent = FindEventOrThrow(id);
        eventStore.Remove(existingEvent);
        return true;
    }
}