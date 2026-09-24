using Events.Domain.Entities;
using Events.Infrastructure.Persistence;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class ProcessedBookingRepositoryTests(PostgresContainerFixture fixture)
    : DatabaseTestBase<EventDbContext>(fixture, "events_db_test")
{
    protected override EventDbContext CreateContext(DbContextOptions<EventDbContext> options) => new(options);

    private async Task<Event> CreatePersistedEvent(int totalSeats = 5)
    {
        var ev = Event.Create("Concert", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), totalSeats);
        await new EventRepository(Context).AddAsync(ev);
        return ev;
    }

    private async Task SaveProcessed(Guid bookingId, Guid eventId)
    {
        await using var context = NewContext();
        context.ProcessedBookings.Add(ProcessedBooking.Create(bookingId, eventId, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueOnlyForProcessedBooking()
    {
        var bookingId = Guid.NewGuid();
        await SaveProcessed(bookingId, Guid.NewGuid());
        var repository = new ProcessedBookingRepository(NewContext());

        Assert.True(await repository.ExistsAsync(bookingId));
        Assert.False(await repository.ExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AnyForEventAsync_ReturnsTrueOnlyForEventWithProcessedBookings()
    {
        var eventId = Guid.NewGuid();
        await SaveProcessed(Guid.NewGuid(), eventId);
        var repository = new ProcessedBookingRepository(NewContext());

        Assert.True(await repository.AnyForEventAsync(eventId));
        Assert.False(await repository.AnyForEventAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_DoesNotSaveByItself()
    {
        var bookingId = Guid.NewGuid();

        new ProcessedBookingRepository(Context).Add(ProcessedBooking.Create(bookingId, Guid.NewGuid(), DateTime.UtcNow));

        Assert.False(await new ProcessedBookingRepository(NewContext()).ExistsAsync(bookingId));
    }

    // Так работает обработчик BookingConfirmed: места и отметка о брони — один SaveChanges
    [Fact]
    public async Task Add_ThenEventUpdate_SavesSeatsAndProcessedBookingTogether()
    {
        var ev = await CreatePersistedEvent(totalSeats: 5);
        var bookingId = Guid.NewGuid();

        ev.TryReserveSeats(1);
        new ProcessedBookingRepository(Context).Add(ProcessedBooking.Create(bookingId, ev.Id, DateTime.UtcNow));
        await new EventRepository(Context).UpdateAsync(ev);

        await using var check = NewContext();
        Assert.Equal(4, (await check.Events.SingleAsync(e => e.Id == ev.Id)).AvailableSeats);
        Assert.True(await new ProcessedBookingRepository(check).ExistsAsync(bookingId));
    }

    // Последняя линия защиты идемпотентности: даже при гонке одна бронь не спишет места дважды
    [Fact]
    public async Task DuplicateBookingId_IsRejectedByPrimaryKey_AndSeatsAreNotChanged()
    {
        var ev = await CreatePersistedEvent(totalSeats: 5);
        var bookingId = Guid.NewGuid();
        await SaveProcessed(bookingId, ev.Id);

        await using var context = NewContext();
        var tracked = await context.Events.SingleAsync(e => e.Id == ev.Id);
        tracked.TryReserveSeats(1);
        context.ProcessedBookings.Add(ProcessedBooking.Create(bookingId, ev.Id, DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(5, (await NewContext().Events.SingleAsync(e => e.Id == ev.Id)).AvailableSeats);
    }
}
