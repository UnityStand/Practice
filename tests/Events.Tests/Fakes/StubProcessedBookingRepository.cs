using Events.Application.Abstractions;
using Events.Domain.Entities;

namespace Events.Tests.Fakes;

// У событий нет обработанных броней, поэтому удаление всегда разрешено.
public sealed class StubProcessedBookingRepository : IProcessedBookingRepository
{
    public Task<bool> ExistsAsync(Guid bookingId) => Task.FromResult(false);
    public Task<bool> AnyForEventAsync(Guid eventId) => Task.FromResult(false);
    public void Add(ProcessedBooking processedBooking) { }
}
