using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<bool> ExistsForEventAsync(Guid eventId);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task<List<Guid>> GetPendingIdsAsync();
}