using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MiniWeb.Middleware;

public class TimingAndLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TimingAndLogMiddleware> _logger;

    public TimingAndLogMiddleware(RequestDelegate next, ILogger<TimingAndLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.Items["RequestId"] as string ?? "unknown";
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "Request processed. RequestId: {requestId}, Method: {method}, Path: {path}, Status: {status}, TimeMs: {timeMs}",
                requestId,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds
            );
        }
    }
}