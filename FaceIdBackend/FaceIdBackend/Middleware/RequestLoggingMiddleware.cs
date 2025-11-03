using System.Diagnostics;

namespace FaceIdBackend.Middleware;

/// <summary>
/// Request/Response logging middleware for debugging and monitoring
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        _logger.LogInformation("→ {Method} {Path} started", requestMethod, requestPath);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 200 && statusCode < 300)
            {
                _logger.LogInformation("← {Method} {Path} completed {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
            else if (statusCode >= 400 && statusCode < 500)
            {
                _logger.LogWarning("← {Method} {Path} completed {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
            else if (statusCode >= 500)
            {
                _logger.LogError("← {Method} {Path} failed {StatusCode} in {Elapsed}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
        }
    }
}
