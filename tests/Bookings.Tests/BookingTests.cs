using System.ComponentModel.DataAnnotations;
using Bookings.Domain.Entities;

namespace Bookings.Tests;

public class BookingTests
{
    private static Booking Pending() =>
        Booking.Create(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

    [Fact]
    public void Confirm_SetsStatusAndProcessedAt()
    {
        var booking = Pending();

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Confirm_Throws_WhenAlreadyConfirmed()
    {
        var booking = Pending();
        booking.Confirm();

        Assert.Throws<ValidationException>(() => booking.Confirm());
    }

    [Fact]
    public void Reject_SetsStatusAndProcessedAt()
    {
        var booking = Pending();

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var booking = Pending();

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Seats_IsOne_ForSingleBooking()
    {
        Assert.Equal(1, Pending().Seats);
    }
}
