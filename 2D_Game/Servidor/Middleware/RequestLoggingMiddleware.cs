using System.Diagnostics;

namespace JuegardoApi.Middleware;

// Logs one line per request: method, path, status code and how long it took.
// Bodies are never logged, so passwords and tokens stay out of the logs.
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        await next(context);
        timer.Stop();
        logger.LogInformation("{Metodo} {Ruta} -> {Estado} ({Ms} ms)",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, timer.ElapsedMilliseconds);
    }
}
