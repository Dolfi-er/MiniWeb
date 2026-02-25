namespace MiniWeb.Errors;

public abstract class DomainException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected DomainException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}