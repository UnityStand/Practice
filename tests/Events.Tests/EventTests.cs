using System.ComponentModel.DataAnnotations;
using Events.Domain.Entities;

namespace Events.Tests;

public class EventTests
{
    private static Event CreateEvent(int totalSeats) =>
        Event.Create("Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), totalSeats);

    [Fact]
    public void Create_Throws_WhenEndIsBeforeStart()
    {
        Assert.Throws<ValidationException>(() =>
            Event.Create("Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), 10));
    }

    [Fact]
    public void TryReserveSeats_DecreasesAvailableSeats()
    {
        var ev = CreateEvent(totalSeats: 5);

        Assert.True(ev.TryReserveSeats(2));
        Assert.Equal(3, ev.AvailableSeats);
    }

    [Fact]
    public void TryReserveSeats_ReturnsFalse_AndKeepsSeats_WhenNotEnough()
    {
        var ev = CreateEvent(totalSeats: 2);

        Assert.False(ev.TryReserveSeats(3));
        Assert.Equal(2, ev.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_CannotExceedTotalSeats()
    {
        var ev = CreateEvent(totalSeats: 2);

        Assert.False(ev.ReleaseSeats());
        Assert.Equal(2, ev.AvailableSeats);
    }
}
