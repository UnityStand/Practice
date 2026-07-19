namespace ASP.NET_Core_Web_API.Exceptions;

public class NotFoundException(string message) : Exception(message);