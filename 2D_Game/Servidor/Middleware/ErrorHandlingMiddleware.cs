using JuegardoApi.Dtos;

namespace JuegardoApi.Middleware;

// First in the pipeline: catches any unhandled exception, logs it and answers with a generic
// 500 so internal details (stack traces, SQL, paths) never reach the client.
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorDto("Ha ocurrido un error en el servidor."));
        }
    }
}
