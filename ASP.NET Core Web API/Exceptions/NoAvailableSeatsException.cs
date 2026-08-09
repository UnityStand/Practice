namespace ASP.NET_Core_Web_API.Exceptions;

public class NoAvailableSeatsException(string message) : Exception(message);