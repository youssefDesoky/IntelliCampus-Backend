using IntelliCampus.Service.Exceptions;
using Microsoft.AspNetCore.Mvc;

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
                //logging the exception can be done here
                _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");

                //return custom error response
               
                var problem = new ProblemDetails()
                {
                    Title = "Error While Processing HTTP Request",
                    Detail = ex.Message,
                    Instance = httpContext.Request.Path,
                    Status = ex switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        ForbiddenException => StatusCodes.Status403Forbidden,
                        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                        InvalidOperationException => StatusCodes.Status400BadRequest,
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
