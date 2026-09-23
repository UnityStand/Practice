using Bookings.Domain.Entities;

namespace Bookings.Application.Services;

public interface IBookingService
{
    Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin);
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}
