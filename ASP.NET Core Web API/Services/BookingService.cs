using ASP.NET_Core_Web_API.Models;
namespace ASP.NET_Core_Web_API.Services;

public class BookingService (IBookingStore bookingStore,IEventService eventService) : IBookingService 
{

  
    public Task<Booking?>  CreateBookingAsync(Guid eventId)
    {
        if (eventService.GetEventById(eventId) is null) return Task.FromResult<Booking?>(null);
        var result = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.Now
        };
        bookingStore.AddBooking(result);
        return Task.FromResult(result);

    }

    public Task<Booking?>  GetBookingByIdAsync(Guid bookingId)
    {
        var result = bookingStore.GetBooking(bookingId);
        return Task.FromResult(result);
    }
}