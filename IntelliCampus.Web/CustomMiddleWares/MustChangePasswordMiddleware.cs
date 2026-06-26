using System.Security.Claims;

namespace IntelliCampus.Web.CustomMiddleWares;

public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var mustChangeClaim = user.FindFirst("must_change_password")?.Value;
            if (mustChangeClaim == "true")
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                var isAllowed = path.Contains("/api/auth/first-time-setup")
                    || path.Contains("/api/auth/logout")
                    || path.Contains("/api/auth/me")
                    || path.Contains("/api/auth/login")
                    || path.Contains("/api/auth/get-credentials")
                    || path.Contains("/api/auth/forgot-password")
                    || path.Contains("/api/auth/reset-password")
                    || path.Contains("/api/faculties/public");

                if (!isAllowed)
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "must_change_password",
                        title = "Password change required",
                        detail = "You must change your password and set a recovery email before accessing the application.",
                        status = 403
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
