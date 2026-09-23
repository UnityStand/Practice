using Events.Application.Abstractions;
using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Persistence;

public class ProcessedBookingRepository(EventDbContext context): IProcessedBookingRepository
{
    public async Task<bool> ExistsAsync(Guid bookingId)
    {
        return await context.ProcessedBookings.AnyAsync(x => x.BookingId == bookingId); 
    }

    public async Task<bool> AnyForEventAsync(Guid eventId)
    {
        return await context.ProcessedBookings.AnyAsync(x => x.EventId == eventId); 
    }

    public void Add(ProcessedBooking processedBooking)
    {
        context.ProcessedBookings.Add(processedBooking);

    }
}