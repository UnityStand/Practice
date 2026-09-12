using EventApi.Domain.Entities;

namespace EventApi.Application.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}
