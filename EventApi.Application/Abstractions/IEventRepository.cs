using EventApi.Domain.Entities;

namespace EventApi.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<(List<Event> Items, int TotalCount)> GetPagedAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task AddAsync(Event @event);
    Task UpdateAsync(Event @event);
    Task RemoveAsync(Event @event);
}