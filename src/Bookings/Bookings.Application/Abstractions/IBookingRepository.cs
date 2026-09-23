using Bookings.Domain.Entities;

namespace Bookings.Application.Abstractions;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<int> CountActiveBookingsByUserIdAsync(Guid userId);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task<List<Guid>> GetPendingIdsAsync();
}