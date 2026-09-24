using System.ComponentModel.DataAnnotations;
using Bookings.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Exceptions;

public class GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {

        logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            BookingLimitExceededException => StatusCodes.Status409Conflict,

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