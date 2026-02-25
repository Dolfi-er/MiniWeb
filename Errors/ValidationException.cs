namespace MiniWeb.Errors;

public sealed class ValidationException : DomainException
{
    public ValidationException(string message) : base("Validation", message, 400) {}
}