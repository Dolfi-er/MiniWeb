using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using MiniWeb.Domain;
using MiniWeb.Errors;
using MiniWeb.Middleware;
using MiniWeb.Services;

var builder = WebApplication.CreateBuilder(args);

//Регестрируем репозиторий как синглтон (одно хранилище для всего приложения)
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();

//Настраиваем сериализацию JSON
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

//Добавляем middleware в порядке обработки
app.UseMiddleware<RequestIdMiddleware>(); //1. Устанавливаем requestId
app.UseMiddleware<ErrorHandlingMiddleware>(); //2. Обрабатываем ошибки
app.UseMiddleware<TimingAndLogMiddleware>(); //3. Логирум время обработки запроса

//Определяем эндпоинты
app.MapGet("/api/books", (IBookRepository repo) =>
{
    var books = repo.GetAll();
    return Results.Ok(books);
});

app.MapGet("/api/books/{id:guid}", (Guid id, IBookRepository repo) =>
{
    var book = repo.GetById(id);
    if (book == null)
    {
        throw new NotFoundException($"Book with id {id} not found");
    }
    return Results.Ok(book);
});

app.MapPost("/api/books", (HttpContext ctx, CreateBookRequest request, IBookRepository repo) =>
{
    //
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        throw new ValidationException("Title is required");
    }

    if (request.Year < 1450 || request.Year > DateTime.Now.Year)
    {
        throw new ValidationException("Year must be between 1450 and current year");
    }

    var created = repo.Create(request.Title.Trim(), request.Author?.Trim() ?? "", request.Year);

    var location = $"/api/books/{created.Id}";
    ctx.Response.Headers.Location = location;

    return Results.Created(location, created);
});

app.Run();