using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(int eventId);
    Task<Booking> GetBookingByIdAsync(int bookingId);
}
