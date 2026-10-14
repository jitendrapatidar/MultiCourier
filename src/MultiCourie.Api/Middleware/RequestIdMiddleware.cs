namespace MultiCourie.Api.Middleware;
/// <summary>
///RequestIdMiddleware
/// </summary>
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";
    public const string ItemKey = "RequestId";

    private readonly RequestDelegate _next;

    /// <summary>
    /// RequestIdMiddleware
    /// </summary>
    /// <param name="next"></param>
    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// InvokeAsync
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = requestId;
        context.Response.Headers[HeaderName] = requestId;

        await _next(context);
    }
}
