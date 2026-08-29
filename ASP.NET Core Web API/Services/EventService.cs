using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace ASP.NET_Core_Web_API.Services;

internal class EventService(AppDbContext context) : IEventService
{

    private async Task<Event> FindEventOrThrow(Guid id)
    {
        var result = await context.Events.FindAsync(id);
        return result ?? throw new NotFoundException($"Event with id {id} not found");
    }

    public async Task<PaginatedResult<Event>> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var query = context.Events.AsQueryable();
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        if (from != null)
            query = query.Where(e => e.StartAt >= from);
        if (to != null)
            query = query.Where(e => e.EndAt <= to);
        var total = await query.CountAsync();
        var items = await query.OrderBy(e => e.StartAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedResult<Event>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event> GetEventById(Guid id)
    {
        return await FindEventOrThrow(id);
    }


    public async Task<Event> CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var newEvent = Event.Create(title, description, startAt, endAt, totalSeats);
        context.Events.Add(newEvent);
        await context.SaveChangesAsync();

        return newEvent;
    }

    public async Task<Event> UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
  startAt = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);                                                                         
  endAt = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);    
        var existingEvent = await FindEventOrThrow(id);
        existingEvent.UpdateInfo(title, description, startAt, endAt);

        await context.SaveChangesAsync();

        return existingEvent;
    }

    public async Task<bool> DeleteEvent(Guid id)
    {
        var existingEvent = await FindEventOrThrow(id);
        if (await context.Bookings.AnyAsync(b =>
                b.EventId == id)) throw new EventHasBookingsException("Cannot delete event with active bookings");
        context.Events.Remove(existingEvent);
        await context.SaveChangesAsync();

        return true;
    }
}