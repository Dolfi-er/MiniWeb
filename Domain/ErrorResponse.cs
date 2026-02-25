namespace MiniWeb.Domain;

public record ErrorResponse(string Code, string Message, string RequestId);
