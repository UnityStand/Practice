using Bookings.Application.Abstractions;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Persistence;

public class BookingRepository(BookingDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await context.Bookings.FindAsync(id).AsTask();
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

    public async Task<int> CountActiveBookingsByUserIdAsync(Guid userId)
    {
        return await context.Bookings
            .Where(b => b.UserId == userId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .CountAsync();
    }
    public Task<List<Guid>> GetPendingIdsAsync()
    {
        return context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id).ToListAsync();
    }
}