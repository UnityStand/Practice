using ASP.NET_Core_Web_API.Models;


namespace ASP.NET_Core_Web_API.DataAccess;

public interface IEventRepository
{
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<(List<Event> Items, int TotalCount)> GetPagedAsync(
        string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task AddAsync(Event @event);
    Task UpdateAsync(Event @event);
    Task RemoveAsync(Event @event);
}