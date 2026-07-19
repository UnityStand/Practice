using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public interface IBookingService
{
   Task<Booking?> CreateBookingAsync(Guid eventId);
   Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}