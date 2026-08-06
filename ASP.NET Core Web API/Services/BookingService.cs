using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class BookingService(IBookingStore bookingStore, IEventService eventService) : IBookingService
{
    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        eventService.GetEventById(eventId);

        var booking = new Booking
        {
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.Now
        };
        bookingStore.AddBooking(booking);
        return Task.FromResult(booking);
    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = bookingStore.GetBooking(bookingId);
        if (booking is null) throw new NotFoundException($"Бронь с id {bookingId} не найдена");

        return Task.FromResult(booking);
    }
}
