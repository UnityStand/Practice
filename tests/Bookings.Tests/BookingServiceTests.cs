using System.ComponentModel.DataAnnotations;
using Bookings.Application.Services;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;

namespace Bookings.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly TestServices _services = new();

    public void Dispose() => _services.Dispose();

    private IBookingService Service() => _services.Create<IBookingService>();

    [Fact]
    public async Task CreateBookingAsync_ReturnsPendingBooking()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = await Service().CreateBookingAsync(eventId, userId);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(userId, booking.UserId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    // Bookings не обращается к сервису Events: существование события здесь не проверяется,
    // несуществующее событие отсеет подписчик в Events (пропуск с логированием).
    [Fact]
    public async Task CreateBookingAsync_DoesNotCallEventsService_AcceptsAnyEventId()
    {
        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_AssignsUniqueIds()
    {
        var eventId = Guid.NewGuid();

        var first = await Service().CreateBookingAsync(eventId, Guid.NewGuid());
        var second = await Service().CreateBookingAsync(eventId, Guid.NewGuid());

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenUserReachesLimit()
    {
        var userId = Guid.NewGuid();
        for (var i = 0; i < TestServices.MaxActiveBookingsPerUser; i++)
            await Service().CreateBookingAsync(Guid.NewGuid(), userId);

        await Assert.ThrowsAsync<BookingLimitExceededException>(
            () => Service().CreateBookingAsync(Guid.NewGuid(), userId));
    }

    [Fact]
    public async Task CreateBookingAsync_LimitsAreIndependentBetweenUsers()
    {
        var userA = Guid.NewGuid();
        for (var i = 0; i < TestServices.MaxActiveBookingsPerUser; i++)
            await Service().CreateBookingAsync(Guid.NewGuid(), userA);

        var bookingForB = await Service().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(BookingStatus.Pending, bookingForB.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_CancelledBookingsDoNotCountTowardsLimit()
    {
        var userId = Guid.NewGuid();
        Booking? last = null;
        for (var i = 0; i < TestServices.MaxActiveBookingsPerUser; i++)
            last = await Service().CreateBookingAsync(Guid.NewGuid(), userId);

        await Service().CancelBookingAsync(last!.Id, userId, isAdmin: false);

        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), userId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_UnderConcurrency_RespectsLimit()
    {
        var userId = Guid.NewGuid();
        var success = 0;
        var limitExceeded = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            try
            {
                await Service().CreateBookingAsync(Guid.NewGuid(), userId);
                Interlocked.Increment(ref success);
            }
            catch (BookingLimitExceededException)
            {
                Interlocked.Increment(ref limitExceeded);
            }
        }));
        await Task.WhenAll(tasks);

        Assert.Equal(TestServices.MaxActiveBookingsPerUser, success);
        Assert.Equal(20 - TestServices.MaxActiveBookingsPerUser, limitExceeded);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBooking()
    {
        var created = await Service().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        var result = await Service().GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetBookingByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelBookingAsync_OwnerCancelsOwnBooking()
    {
        var ownerId = Guid.NewGuid();
        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), ownerId);

        await Service().CancelBookingAsync(booking.Id, ownerId, isAdmin: false);

        Assert.Equal(BookingStatus.Cancelled, (await Service().GetBookingByIdAsync(booking.Id)).Status);
    }

    [Fact]
    public async Task CancelBookingAsync_Throws_WhenNotOwnerAndNotAdmin()
    {
        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => Service().CancelBookingAsync(booking.Id, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsSomeoneElsesBooking()
    {
        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        await Service().CancelBookingAsync(booking.Id, Guid.NewGuid(), isAdmin: true);

        Assert.Equal(BookingStatus.Cancelled, (await Service().GetBookingByIdAsync(booking.Id)).Status);
    }

    [Fact]
    public async Task CancelBookingAsync_Throws_WhenAlreadyCancelled()
    {
        var ownerId = Guid.NewGuid();
        var booking = await Service().CreateBookingAsync(Guid.NewGuid(), ownerId);
        await Service().CancelBookingAsync(booking.Id, ownerId, isAdmin: false);

        await Assert.ThrowsAsync<ValidationException>(
            () => Service().CancelBookingAsync(booking.Id, ownerId, isAdmin: false));
    }

    [Fact]
    public async Task CancelBookingAsync_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Service().CancelBookingAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: true));
    }
}
