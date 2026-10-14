namespace MultiCourie.Api.Models;
/// <summary>
/// ApiErrorResponse
/// </summary>
public sealed class ApiErrorResponse
{
    public required ApiErrorBody Error { get; init; }
}
/// <summary>
/// ApiErrorBody
/// </summary>
public sealed class ApiErrorBody
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public required string RequestId { get; init; }

    public IReadOnlyList<FieldErrorDetail> Details { get; init; } = Array.Empty<FieldErrorDetail>();
}
/// <summary>
/// FieldErrorDetail
/// </summary>
/// <param name="Field"></param>
/// <param name="Message"></param>
public sealed record FieldErrorDetail(string Field, string Message);
