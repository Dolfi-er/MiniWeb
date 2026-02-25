namespace MiniWeb.Domain;

public record CreateBookRequest(string Title, string Author, int Year);