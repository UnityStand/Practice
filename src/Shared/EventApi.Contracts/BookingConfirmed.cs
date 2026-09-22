namespace EventApi.Contracts;                                                                                                         
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    DateTime Confirmed,
    int BookedSeats
    );