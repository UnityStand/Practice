using EventApi.Contracts;
using Events.Application.Abstractions;
using Events.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Events.Application.Messaging;

public class BookingConfirmedHandler(
    IEventRepository eventRepository,
    ILogger<BookingConfirmedHandler>  logger,
    IProcessedBookingRepository processedBookingRepository) : IBookingConfirmedHandler
{
    public async Task HandleAsync(BookingConfirmed message)
    {
        if (await processedBookingRepository.ExistsAsync(message.BookingId))
        {
            logger.LogInformation("Booking {BookingId} already processed", message.BookingId);
            return;
        }

        var @event = await eventRepository.GetEventByIdAsync(message.EventId);
        if (@event == null)
        {
            logger.LogWarning("Failed to find Event {EventId}", message.EventId);
            return;
        }

        if (!@event.TryReserveSeats(message.BookedSeats))
        {
            logger.LogWarning("Failed to book seats count{BookedSeats}, available count {AvailableSeats}",
                message.BookedSeats, @event.AvailableSeats);
            return;
        }


        processedBookingRepository.Add(ProcessedBooking.Create(message.BookingId, @event.Id, DateTime.UtcNow));
        await eventRepository.UpdateAsync(@event);
        logger.LogInformation("Booking {BookingId} confirmed for Event {EventId}", message.BookingId, message.EventId);
    }
}