using EventApi.Application.Abstractions;
using EventApi.Application.Options;
using EventApi.Domain.Entities;
using EventApi.Domain.Exceptions;
using Microsoft.Extensions.Options;     

namespace EventApi.Application.Services;

public class BookingService(IEventRepository
    eventRepository, IBookingRepository bookingRepository, IOptions<BookingSettings> bookingSettings)  : IBookingService
{      private int MaxActiveBookingsPerUser => bookingSettings.Value.MaxActiveBookingsPerUser;                   
    private static readonly SemaphoreSlim _bookingLock = new(1, 1);
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingLock.WaitAsync();

        try
        {
            var @event = await eventRepository.GetEventByIdAsync(eventId);
            if (@event == null)
                throw new NotFoundException($"Event with id {eventId} not found");
            if (@event.StartAt <= DateTime.UtcNow)                                                                        
                throw new EventAlreadyStartedException("Cannot book an event that has already started");   
            var activeCount = await bookingRepository.CountActiveBookingsByUserIdAsync(userId);                           
            if (activeCount >= MaxActiveBookingsPerUser)                                                                  
                throw new BookingLimitExceededException($"Booking limit exceeded: maximum {MaxActiveBookingsPerUser} active bookings allowed");     
            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            var booking = Booking.Create(eventId, userId, BookingStatus.Pending, DateTime.UtcNow);
            await bookingRepository.AddAsync(booking);
            return booking;

        }
        finally
        {
            _bookingLock.Release();
        }


    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId);
        return booking ?? throw new NotFoundException($"Booking with id {bookingId} not found");
    }
    
public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole)                          
    {                                                                                                             
        var booking = await bookingRepository.GetByIdAsync(bookingId)                                                       ?? throw new NotFoundException($"Booking with id {bookingId} not found");                             
                                                                                                                
        if (booking.UserId != userId && userRole != UserRole.Admin)                                               
            throw new ForbiddenException("You do not have permission to cancel this booking");                    
                                                                                                                
        booking.Cancel();                                                                                         
        var @event = await eventRepository.GetEventByIdAsync(booking.EventId);                                         
        @event!.ReleaseSeats();                                                                                       
        await eventRepository.UpdateAsync(@event);                                                                    
                                                                                                                
        await bookingRepository.UpdateAsync(booking);                                                  
    }     
}