namespace EventApi.Domain.Exceptions;

public class BookingLimitExceededException(string message) : Exception(message);
