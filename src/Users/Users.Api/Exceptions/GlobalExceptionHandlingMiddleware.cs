using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Users.Api.Exceptions;

public class GlobalExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {

        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError
        };

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = statusCode == StatusCodes.Status500InternalServerError ?
                "An unexpected error occurred"
                : exception.Message
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);

        return true;

    }

}