using EventApi.Application.Abstractions;
using EventApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Infrastructure.Persistence;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await context.Bookings.FindAsync(id).AsTask();
    }

    public async Task<bool> ExistsForEventAsync(Guid eventId)
    {
        return await context.Bookings.AnyAsync(b =>
            b.EventId == eventId);
    }

    public async Task AddAsync(Booking booking)
    {
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        await context.SaveChangesAsync();
    }

    public Task<List<Guid>> GetPendingIdsAsync()
    {
        return context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id).ToListAsync();
    }
}