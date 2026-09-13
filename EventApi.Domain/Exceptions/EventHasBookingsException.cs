namespace EventApi.Domain.Exceptions;

public class EventHasBookingsException(string message) : Exception(message);
