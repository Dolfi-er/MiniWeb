using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiniWeb.Domain;
using MiniWeb.Errors;

namespace MiniWeb.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        //Получаем requestId из items
        var requestId = context.Items["RequestId"] as string ?? "unknown";

        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            //Отправляем ответ с ошибкой
            _logger.LogWarning(ex, "Domain error occured. RequestId: {requestId}", requestId);
            await WriteErrorResponse(context, ex.StatusCode, ex.Code, ex.Message, requestId);
        }
        catch (Exception ex)
        {
            //Отправляем ответ о непредвиденной ошибке
            _logger.LogError(ex, "Unexpected error occured. RequestId: {requestId}", requestId);
            await WriteErrorResponse(context, 500, "internal_error", "An internal error has occured", requestId);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string code, string message, string requestId)
    {   
        //Если ответ уже начат, дальше не пишем
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var errorResponse = new ErrorResponse(code, message, requestId);
        var json = JsonSerializer.Serialize(errorResponse);

        await context.Response.WriteAsync(json);
    }
}