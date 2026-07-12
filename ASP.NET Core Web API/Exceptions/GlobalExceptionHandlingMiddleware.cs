using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Exceptions;

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
          // 1. залогировать exception через _logger
          _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);        
          // 2. определить статус-код в зависимости от типа exception
          var statusCode = exception switch
          {
              ValidationException => StatusCodes.Status400BadRequest,
              NotFoundException => StatusCodes.Status404NotFound,

              _ => StatusCodes.Status500InternalServerError                                                                                   
          };
          //  3. собрать ProblemDetails (Status, Title/Detail)
          var error= new ProblemDetails{
              Status = statusCode, 
              Detail = statusCode == StatusCodes.Status500InternalServerError? 
                  "Произошла непредвиденная ошибка"
                  : exception.Message};
          //  4. httpContext.Response.StatusCode = ...
          httpContext.Response.StatusCode = statusCode;           
          //  5. записать тело ответа, например через httpContext.Response.WriteAsJsonAsync(...)
          await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);                                                     
          //  6. вернуть true — значит "я обработал", ASP.NET не будет искать другой обработчик
          return true;
          
      }                                                                                          
          
  }    