using System.Net;
using System.Net.Http.Headers;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
/// A delegating handler that adds Urbane Bolt authentication to outgoing HTTP requests and handles token refresh on unauthorized responses.
/// </summary>
public sealed class UrbaneBoltAuthHandler : DelegatingHandler
{
    private readonly UrbaneBoltTokenProvider _tokenProvider;

    public UrbaneBoltAuthHandler(UrbaneBoltTokenProvider tokenProvider) => _tokenProvider = tokenProvider;
    /// <summary>
    /// Sends an HTTP request with Urbane Bolt authentication, refreshing the token if the response indicates unauthorized access.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            _tokenProvider.Invalidate();
            token = await _tokenProvider.GetTokenAsync(cancellationToken, forceRefresh: true);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
