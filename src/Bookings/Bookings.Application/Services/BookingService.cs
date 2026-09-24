using Bookings.Application.Abstractions;
using Bookings.Application.Options;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Bookings.Application.Services;

public class BookingService(IBookingRepository bookingRepository, IOptions<BookingSettings> bookingSettings) : IBookingService
{
    private int MaxActiveBookingsPerUser => bookingSettings.Value.MaxActiveBookingsPerUser;
    private static readonly SemaphoreSlim _bookingLock = new(1, 1);
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await _bookingLock.WaitAsync();

        try
        {


            var activeCount = await bookingRepository.CountActiveBookingsByUserIdAsync(userId);
            if (activeCount >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException($"Booking limit exceeded: maximum {MaxActiveBookingsPerUser} active bookings allowed");

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

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId) ?? throw new NotFoundException($"Booking with id {bookingId} not found");

        if (booking.UserId != userId && !isAdmin)
            throw new ForbiddenException("You do not have permission to cancel this booking");

        booking.Cancel();

        await bookingRepository.UpdateAsync(booking);
    }
}