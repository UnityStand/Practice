using EventApi.Contracts;
using Events.Application.Abstractions;
using Events.Application.Caching;
using Events.Application.Services;
using Events.Domain.Entities;
using Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Events.Tests;

public class BookingConfirmedHandlerTests : IDisposable
{
    private readonly TestServices _services = new();

    public void Dispose() => _services.Dispose();

    private async Task<Guid> CreateEvent(int totalSeats)
    {
        var context = _services.Create<EventDbContext>();
        var ev = Event.Create("Concert", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), totalSeats);
        context.Events.Add(ev);
        await context.SaveChangesAsync();
        return ev.Id;
    }

    private static BookingConfirmed Message(Guid eventId, int seats = 1, Guid? bookingId = null) =>
        new(BookingId: bookingId ?? Guid.NewGuid(),
            EventId: eventId,
            UserId: Guid.NewGuid(),
            Confirmed: DateTime.UtcNow,
            BookedSeats: seats);

    // Каждое сообщение — в своём scope, как в BookingConfirmedConsumer
    private Task Handle(BookingConfirmed message) =>
        _services.Create<IBookingConfirmedHandler>().HandleAsync(message);

    private async Task<int> AvailableSeats(Guid eventId) =>
        (await _services.Create<EventDbContext>().Events.SingleAsync(e => e.Id == eventId)).AvailableSeats;

    private Task<int> ProcessedCount() =>
        _services.Create<EventDbContext>().ProcessedBookings.CountAsync();

    [Fact]
    public async Task Handle_ReservesSeats_AndMarksBookingProcessed()
    {
        var eventId = await CreateEvent(totalSeats: 5);
        var message = Message(eventId);

        await Handle(message);

        Assert.Equal(4, await AvailableSeats(eventId));
        var processed = await _services.Create<EventDbContext>().ProcessedBookings.SingleAsync();
        Assert.Equal(message.BookingId, processed.BookingId);
        Assert.Equal(eventId, processed.EventId);
    }

    [Fact]
    public async Task Handle_ReservesBookedSeatsCount()
    {
        var eventId = await CreateEvent(totalSeats: 10);

        await Handle(Message(eventId, seats: 3));

        Assert.Equal(7, await AvailableSeats(eventId));
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenSameMessageDeliveredTwice()
    {
        var eventId = await CreateEvent(totalSeats: 5);
        var message = Message(eventId);

        await Handle(message);
        await Handle(message);

        Assert.Equal(4, await AvailableSeats(eventId));
        Assert.Equal(1, await ProcessedCount());
    }

    [Fact]
    public async Task Handle_ProcessesDifferentBookingsForSameEvent()
    {
        var eventId = await CreateEvent(totalSeats: 5);

        await Handle(Message(eventId));
        await Handle(Message(eventId));
        await Handle(Message(eventId));

        Assert.Equal(2, await AvailableSeats(eventId));
        Assert.Equal(3, await ProcessedCount());
    }

    [Fact]
    public async Task Handle_SkipsWithoutThrowing_WhenEventNotFound()
    {
        await Handle(Message(Guid.NewGuid()));

        Assert.Equal(0, await ProcessedCount());
    }

    [Fact]
    public async Task Handle_InvalidatesEventCache_AfterReservingSeats()
    {
        var eventId = await CreateEvent(totalSeats: 5);
        await _services.Create<IEventService>().GetEventById(eventId); // прогреваем кеш: 5 свободных мест

        await Handle(Message(eventId));

        Assert.False(_services.Cache.Contains(CacheKeys.Event(eventId)));
        Assert.Equal(4, (await _services.Create<IEventService>().GetEventById(eventId)).AvailableSeats);
    }

    [Fact]
    public async Task Handle_KeepsEventCache_WhenNotEnoughSeats()
    {
        var eventId = await CreateEvent(totalSeats: 1);
        await Handle(Message(eventId));
        await _services.Create<IEventService>().GetEventById(eventId);

        await Handle(Message(eventId)); // мест нет, данные не меняются

        Assert.True(_services.Cache.Contains(CacheKeys.Event(eventId)));
    }

    [Fact]
    public async Task Handle_SkipsWithoutThrowing_WhenNotEnoughSeats()
    {
        var eventId = await CreateEvent(totalSeats: 1);

        await Handle(Message(eventId));
        await Handle(Message(eventId));

        Assert.Equal(0, await AvailableSeats(eventId));
        Assert.Equal(1, await ProcessedCount());
    }
}
