using MiniWeb.Domain;

namespace MiniWeb.Services;

public interface IBookRepository
{
    IReadOnlyCollection<Book> GetAll();
    Book? GetById(Guid id);
    Book Create(string Title, string Author, int Year);
}