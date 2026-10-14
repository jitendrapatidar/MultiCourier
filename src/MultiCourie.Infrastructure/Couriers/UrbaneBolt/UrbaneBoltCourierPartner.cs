using Microsoft.Extensions.Logging;
using MultiCourie.Application.Common;
using MultiCourie.Application.Couriers;
using MultiCourie.Application.Orders.Models;
using MultiCourie.Domain.Enums;
using System.Net;
using System.Text;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
/// Represents the Urbane Bolt courier partner and provides methods to create, track, and cancel shipments through the Urbane Bolt API.
/// </summary>
public sealed class UrbaneBoltCourierPartner : ICourierPartner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrbaneBoltOrderMapper _mapper;
    private readonly ILogger<UrbaneBoltCourierPartner> _logger;

    public UrbaneBoltCourierPartner(
        IHttpClientFactory httpClientFactory,
        UrbaneBoltOrderMapper mapper,
        ILogger<UrbaneBoltCourierPartner> logger)
    {
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public string PartnerKey => "urbanebolt";
    /// <summary>
    /// Creates a shipment with Urbane Bolt for the given normalized order.
    /// </summary>
    /// <param name="order"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CourierCreateResult> CreateShipmentAsync(NormalizedOrder order, CancellationToken cancellationToken = default)
    {
        var requestPayload = _mapper.BuildManifestPayload(order);
        var client = _httpClientFactory.CreateClient("UrbaneBolt");

        using var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("/api/v1/services/manifest/", content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UrbaneBolt manifest network failure for order {OrderId}", order.OrderId);
            throw;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = UrbaneBoltResponseParser.ParseManifest(responseBody);

        if (!response.IsSuccessStatusCode)
        {
            return new CourierCreateResult
            {
                Success = false,
                Status = ShipmentStatus.Failed,
                RequestPayload = requestPayload,
                ResponsePayload = responseBody,
                ErrorCode = MapHttpStatus(response.StatusCode),
                ErrorMessage = parsed.Error ?? "Courier rejected the shipment request."
            };
        }

        if (!parsed.Success)
        {
            return new CourierCreateResult
            {
                Success = false,
                Status = ShipmentStatus.Failed,
                RequestPayload = requestPayload,
                ResponsePayload = responseBody,
                ErrorCode = ErrorCodes.CourierClientError,
                ErrorMessage = parsed.Error ?? "Courier rejected the shipment request."
            };
        }

        return new CourierCreateResult
        {
            Success = true,
            AwbNumber = parsed.Awb,
            CourierOrderId = parsed.OrderNumber ?? order.OrderId,
            Status = parsed.Status,
            RequestPayload = requestPayload,
            ResponsePayload = responseBody
        };
    }
    /// <summary>
    /// Tracks a shipment with Urbane Bolt using the provided AWB number.
    /// </summary>
    /// <param name="awbNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CourierTrackResult> TrackShipmentAsync(string awbNumber, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("UrbaneBolt");
        var response = await client.GetAsync($"/api/v1/services/tracking-pub/?awb={Uri.EscapeDataString(awbNumber)}", cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new CourierTrackResult
            {
                Success = false,
                Status = ShipmentStatus.InTransit,
                ResponsePayload = responseBody,
                ErrorCode = MapHttpStatus(response.StatusCode),
                ErrorMessage = "Unable to fetch tracking from courier."
            };
        }

        var parsed = UrbaneBoltResponseParser.ParseTracking(responseBody);
        return new CourierTrackResult
        {
            Success = parsed.Success,
            Status = parsed.Status,
            CourierStatusText = parsed.StatusText,
            ResponsePayload = responseBody,
            ErrorCode = parsed.Success ? null : ErrorCodes.CourierClientError,
            ErrorMessage = parsed.Error
        };
    }
    /// <summary>
    /// Cancels a shipment with Urbane Bolt using the provided AWB number.
    /// </summary>
    /// <param name="awbNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CourierCancelResult> CancelShipmentAsync(string awbNumber, CancellationToken cancellationToken = default)
    {
        var requestPayload = $"{{\"awbs\":\"{awbNumber}\"}}";
        var client = _httpClientFactory.CreateClient("UrbaneBolt");
        using var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/services/cancel/", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new CourierCancelResult
            {
                Success = false,
                Status = ShipmentStatus.Failed,
                RequestPayload = requestPayload,
                ResponsePayload = responseBody,
                ErrorCode = MapHttpStatus(response.StatusCode),
                ErrorMessage = "Courier rejected cancellation request."
            };
        }

        var parsed = UrbaneBoltResponseParser.ParseCancel(responseBody);
        return new CourierCancelResult
        {
            Success = parsed.Success,
            Status = parsed.Status,
            RequestPayload = requestPayload,
            ResponsePayload = responseBody,
            ErrorCode = parsed.Success ? null : ErrorCodes.CourierClientError,
            ErrorMessage = parsed.Error
        };
    }
    /// <summary>
    /// Maps the HTTP status code to a corresponding error code for the courier partner.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static string MapHttpStatus(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 ? ErrorCodes.CourierUnavailable : ErrorCodes.CourierClientError;
}
