using System.Text;
using Serilog.Context;

namespace AgenticPlatform.API.Middleware;

public sealed class RequestResponseLoggingMiddleware
{
    private const int MaxLoggedBodyLength = 4096;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestBody = await ReadRequestBodyAsync(context);
        var originalBody = context.Response.Body;

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            using (LogContext.PushProperty("RequestBody", requestBody))
            {
                await _next(context);
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.Clear();
            context.Response.StatusCode = 499;
        }
        finally
        {
            var elapsedMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
            var responseText = await ReadResponseBodyAsync(context);

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.0000} ms. RequestBody={RequestBody} ResponseBody={ResponseBody}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs,
                requestBody,
                responseText);

            responseBody.Position = 0;
            if (!context.RequestAborted.IsCancellationRequested)
            {
                await responseBody.CopyToAsync(originalBody);
            }
            context.Response.Body = originalBody;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        if (IsSensitivePath(context.Request.Path) || !CanHaveBody(context.Request.Method))
        {
            return "[not logged]";
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return Truncate(body);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        if (IsSensitivePath(context.Request.Path))
        {
            return "[not logged]";
        }

        var response = context.Response;

        if (response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true
            && response.ContentType?.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return "[not json]";
        }

        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Position = 0;

        return Truncate(body);
    }

    private static bool CanHaveBody(string method)
    {
        return HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);
    }

    private static bool IsSensitivePath(PathString path)
    {
        return path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/v1/ai-settings", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/v1/agents", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/v1/tools", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "[empty]";
        }

        return value.Length <= MaxLoggedBodyLength
            ? value
            : string.Concat(value.AsSpan(0, MaxLoggedBodyLength), "...[truncated]");
    }
}
