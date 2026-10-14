using FluentValidation;
using MultiCourie.Api.Models;
using MultiCourie.Application.Common;
using MultiCourie.Application.Couriers;

namespace MultiCourie.Api.Middleware;

/// <summary>
/// ExceptionHandlingMiddleware
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// ExceptionHandlingMiddleware
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    /// <summary>
    /// InvokeAsync
    /// </summary>
    /// <param name="context"></param>
    /// <param name="courierResolver"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context, CourierPartnerResolver courierResolver)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var requestId = GetRequestId(context);
            _logger.LogWarning("Validation failed. RequestId={RequestId}", requestId);

            var details = ex.Errors
                .Select(e => new FieldErrorDetail(e.PropertyName, e.ErrorMessage))
                .ToList();

            await WriteErrorAsync(context, 400, ErrorCodes.ValidationError, "One or more validation errors occurred.", requestId, details);
        }
        catch (AppException ex)
        {
            var requestId = GetRequestId(context);
            _logger.LogWarning("Application error {ErrorCode}. RequestId={RequestId}", ex.Code, requestId);

            var details = ex.FieldErrors
                .Select(e => new FieldErrorDetail(e.Field, e.Message))
                .ToList();

            var message = ex.Code == ErrorCodes.UnknownCourier
                ? $"{ex.Message} Supported couriers: {string.Join(", ", courierResolver.SupportedPartners)}."
                : ex.Message;

            await WriteErrorAsync(context, ex.StatusCode, ex.Code, message, requestId, details);
        }
        catch (Exception ex)
        {
            var requestId = GetRequestId(context);
            _logger.LogError(ex, "Unhandled error. RequestId={RequestId}", requestId);
            await WriteErrorAsync(context, 500, ErrorCodes.InternalError, "An unexpected error occurred.", requestId, Array.Empty<FieldErrorDetail>());
        }
    }

    private static string GetRequestId(HttpContext context) =>
        context.Items[RequestIdMiddleware.ItemKey]?.ToString() ?? Guid.NewGuid().ToString("N");

    private static Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        string requestId,
        IReadOnlyList<FieldErrorDetail> details)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new ApiErrorResponse
        {
            Error = new ApiErrorBody
            {
                Code = code,
                Message = message,
                RequestId = requestId,
                Details = details
            }
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}
