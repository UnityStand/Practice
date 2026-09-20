using EventApi.Domain.Entities;

namespace EventApi.Application.Services;

public interface IBookingService
{
    Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole);     
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}
