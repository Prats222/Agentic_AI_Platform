namespace AgenticPlatform.API.Middleware;

public sealed class CacheHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public CacheHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
            || !HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache";
            context.Response.Headers.Pragma = "no-cache";
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !context.Response.Headers.ContainsKey("Cache-Control")
            && context.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.Response.Headers.CacheControl = "public,max-age=30";
            context.Response.Headers.Vary = "Authorization";
        }
    }
}
