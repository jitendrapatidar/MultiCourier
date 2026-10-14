namespace MultiCourie.Application.Common;

public class AppException : Exception
{
    public AppException(string code, string message, int statusCode = 400, IReadOnlyList<FieldError>? fieldErrors = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        FieldErrors = fieldErrors ?? Array.Empty<FieldError>();
    }

    public string Code { get; }

    public int StatusCode { get; }

    public IReadOnlyList<FieldError> FieldErrors { get; }
}

public sealed record FieldError(string Field, string Message);
