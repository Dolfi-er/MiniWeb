namespace MiniWeb.Errors;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base("NotFound", message, 404) {}
}