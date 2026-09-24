using EventApi.Contracts;

namespace Events.Application.Abstractions;

public interface IBookingConfirmedHandler
{
    public Task HandleAsync(BookingConfirmed message);
}