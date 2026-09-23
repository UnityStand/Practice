namespace Events.Domain.Exceptions;

public class EventHasBookingsException(string message) : Exception(message);
