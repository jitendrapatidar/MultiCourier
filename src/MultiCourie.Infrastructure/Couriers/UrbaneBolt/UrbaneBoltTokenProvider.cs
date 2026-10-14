using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MultiCourie.Infrastructure.Options;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
/// Provides a mechanism to obtain and cache an authentication token for the Urbane Bolt courier API, handling token expiration and refresh as needed.
/// </summary>
public sealed class UrbaneBoltTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrbaneBoltOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public UrbaneBoltTokenProvider(IHttpClientFactory httpClientFactory, IOptions<UrbaneBoltOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        if (!forceRefresh && _token is not null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _token;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _token is not null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _token;
            }

            ValidateConfiguration();

            var client = _httpClientFactory.CreateClient("UrbaneBoltAuth");
            var payload = new { username = _options.Username, password = _options.Password };
            using var response = await client.PostAsJsonAsync("/api/v1/auth/getToken/", payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"UrbaneBolt authentication failed with status {(int)response.StatusCode}: {GetFailureDetail(body)}");
            }

            _token = ExtractToken(body)
                ?? throw new InvalidOperationException(
                    $"UrbaneBolt authentication response did not contain a token. Response: {GetFailureDetail(body)}");

            _expiresAt = DateTimeOffset.UtcNow.AddMinutes(50);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _expiresAt = DateTimeOffset.MinValue;

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            _options.Password.Equals("YOUR_UAT_PASSWORD", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "UrbaneBolt credentials are not configured. Set Couriers:UrbaneBolt:Username and Password in user secrets or appsettings.Development.json.");
        }
    }

    private static string? ExtractToken(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return FindToken(doc.RootElement);
    }

    private static string? FindToken(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                     property.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase) ||
                     property.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.Value.GetString()))
                {
                    return property.Value.GetString();
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var nestedToken = FindToken(property.Value);
                if (nestedToken is not null)
                {
                    return nestedToken;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedToken = FindToken(item);
                if (nestedToken is not null)
                {
                    return nestedToken;
                }
            }
        }

        return null;
    }

    private static string GetFailureDetail(string body)
    {
        var detail = body.Trim();
        return detail.Length <= 500 ? detail : detail[..500];
    }
}
