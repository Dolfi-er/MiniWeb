using System.Collections.Concurrent;
using MiniWeb.Domain;

namespace MiniWeb.Services;

public class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();

    public IReadOnlyCollection<Book> GetAll()
    {
        return _books.Values
            .OrderBy(i => i.Title)
            .ThenBy(i => i.Year)
            .ToArray();
    }

    public Book? GetById(Guid id)
    {
        _books.TryGetValue(id, out var book);
        return book;
    }

    public Book Create(string Title, string Author, int Year)
    {
        var id = Guid.NewGuid();
        var book = new Book(id, Title, Author, Year);
        _books[id] = book;
        return book;
    }
}