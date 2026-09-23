using Bookings.Application.Services;
using Bookings.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bookings.Tests;

public class BookingBackgroundServiceTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly TestServices _services = new();

    public void Dispose() => _services.Dispose();

    private async Task<Booking> ConfirmViaBackgroundService(Booking booking)
    {
        var worker = _services.Provider.GetServices<IHostedService>().OfType<BookingBackgroundService>().Single();
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await _services.Publisher.WaitForFirstAsync(Timeout);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
        return await _services.Create<IBookingService>().GetBookingByIdAsync(booking.Id);
    }

    [Fact]
    public async Task ConfirmsPendingBooking_AndPublishesItOnce()
    {
        var booking = await _services.Create<IBookingService>().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        var confirmed = await ConfirmViaBackgroundService(booking);

        Assert.Equal(BookingStatus.Confirmed, confirmed.Status);
        Assert.Single(_services.Publisher.Published);
    }

    [Fact]
    public async Task PublishesOnlyAfterStatusIsSavedToDatabase()
    {
        var booking = await _services.Create<IBookingService>().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        await ConfirmViaBackgroundService(booking);

        var (_, statusInDbAtPublishTime) = _services.Publisher.Published.Single();
        Assert.Equal(BookingStatus.Confirmed, statusInDbAtPublishTime);
    }

    [Fact]
    public async Task PublishedMessage_MatchesSavedBooking()
    {
        var booking = await _services.Create<IBookingService>().CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        var confirmed = await ConfirmViaBackgroundService(booking);

        var (message, _) = _services.Publisher.Published.Single();
        Assert.Equal(confirmed.Id, message.BookingId);
        Assert.Equal(confirmed.EventId, message.EventId);
        Assert.Equal(confirmed.UserId, message.UserId);
        Assert.Equal(confirmed.ProcessedAt, message.Confirmed);
        Assert.Equal(confirmed.Seats, message.BookedSeats);
    }
}
