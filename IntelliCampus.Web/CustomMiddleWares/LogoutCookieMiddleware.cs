namespace IntelliCampus.Web.CustomMiddleWares;

public class LogoutCookieMiddleware
{
    private readonly RequestDelegate _next;

    public LogoutCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth/logout", StringComparison.OrdinalIgnoreCase)
            && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Cookies.Delete("token", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax
                });
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
