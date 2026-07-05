using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service_Abstraction.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace IntelliCampus.Web.CustomMiddleWares
{
    public class ExceptionHandlerMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleWare> _logger;
        public ExceptionHandlerMiddleWare(RequestDelegate next, ILogger<ExceptionHandlerMiddleWare> logger)
        {
            _next = next;
            _logger = logger;

        }

        public async Task InvokeAsync(HttpContext httpContext )
        {
            try
            {
                await _next.Invoke(httpContext);
                await HandleNonSuccessStatusAsync(httpContext);


            }
            catch (Exception ex)
            {
                var detail = ex.Message;
                if (ex is DbUpdateException dbEx && dbEx.InnerException is not null)
                    detail = dbEx.InnerException.Message;

                switch (ex)
                {
                    case NotFoundException:
                    case ForbiddenException:
                        _logger.LogWarning(ex, "Expected application exception: {Message}", ex.Message);
                        break;
                    default:
                        _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                        break;
                }

                //return custom error response
               
                var problem = new ProblemDetails()
                {
                    Title = "Error While Processing HTTP Request",
                    Detail = detail,
                    Instance = httpContext.Request.Path,
                    Status = ex switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        ForbiddenException => StatusCodes.Status403Forbidden,
                        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                        InvalidOperationException => StatusCodes.Status400BadRequest,
                        ArgumentException => StatusCodes.Status400BadRequest,
                        RouterNotInitializedException => StatusCodes.Status503ServiceUnavailable,
                        FaheemAiException => StatusCodes.Status502BadGateway,
                        HttpRequestException => StatusCodes.Status503ServiceUnavailable,
                        _ => StatusCodes.Status500InternalServerError
                    }
               };
                httpContext.Response.StatusCode = problem.Status.Value;

                await httpContext.Response.WriteAsJsonAsync(problem);



            }
        }

        private static async Task HandleNonSuccessStatusAsync(HttpContext httpContext)
        {
            if (httpContext.Response.StatusCode == 404 && !httpContext.Response.HasStarted)
            {
                var response = new ProblemDetails()
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Error while Processing The Request  - EndPoint Not Found",
                    Detail = $"EndPoint {httpContext.Request.Path} Not Found",
                    Instance = httpContext.Request.Path
                };
                await httpContext.Response.WriteAsJsonAsync(response);
            }
            else if (httpContext.Response.StatusCode == 403 && !httpContext.Response.HasStarted)
            {
                var response = new ProblemDetails()
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have permission to access this resource.",
                    Instance = httpContext.Request.Path
                };
                await httpContext.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
