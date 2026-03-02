using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MiniWeb.Middleware;

public class RequestIdMiddleware
{
    private static readonly Regex AllowedHeaderPattern = new Regex("^[a-zA-Z0-9-]{1, 64}$");

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        //Id из заголовка X-Request-Id
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();

        //Если заголовок отсутствует или недопустимый, генерируем новый
        if (string.IsNullOrWhiteSpace(requestId) || !AllowedHeaderPattern.IsMatch(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        //Сохраняем в items для доступа внутри запроса
        context.Items["RequestId"] = requestId;

        //Добавляем в ответ, чтобы клиент мог видеть Id
        context.Response.Headers["X-Request-Id"] = requestId;

        await _next(context);
    }
}