namespace JuegardoApi.Middleware;

// Adds security headers to every response, error responses included.
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Added right before the response is sent, so ErrorHandlingMiddleware clearing the
        // response after an exception doesn't remove them.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";     // don't guess content types
            headers["X-Frame-Options"] = "DENY";               // can't be embedded in other pages
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["Cache-Control"] = "no-store";             // responses may contain tokens or private data
            return Task.CompletedTask;
        });
        return next(context);
    }
}
