using EventApi.Domain.Entities;

namespace EventApi.Application.Abstractions;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<int> CountActiveBookingsByUserIdAsync(Guid userId);
    Task<bool> ExistsForEventAsync(Guid eventId);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task<List<Guid>> GetPendingIdsAsync();
}