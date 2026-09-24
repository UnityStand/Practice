using Events.Application.Abstractions;
using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Persistence;

public class EventRepository(EventDbContext context) : IEventRepository
{
    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await context.Events.FindAsync(id).AsTask();
    }

    public async Task<(List<Event> Items, int TotalCount)> GetPagedAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize)
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
        return (items, total);
    }

    public async Task AddAsync(Event @event)
    {
        context.Events.Add(@event);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event @event)
    {
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Event @event)
    {
        context.Events.Remove(@event);
        await context.SaveChangesAsync();
    }
}