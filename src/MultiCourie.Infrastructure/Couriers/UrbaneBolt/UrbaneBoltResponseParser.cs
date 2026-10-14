using System.Text.Json;
using MultiCourie.Domain.Enums;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
/// Provides methods to parse responses from the Urbane Bolt courier API for manifest creation, tracking, and cancellation.
/// </summary>
internal static class UrbaneBoltResponseParser
{
    public static (bool Success, string? Awb, string? OrderNumber, ShipmentStatus Status, string? Error) ParseManifest(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                return ParseManifestItem(root[0], responseBody);
            }

            if (root.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    return ParseManifestItem(data[0], responseBody);
                }

                if (data.ValueKind == JsonValueKind.Object)
                {
                    return ParseManifestItem(data, responseBody);
                }
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                return ParseManifestItem(root, responseBody);
            }
        }
        catch (JsonException)
        {
            return (false, null, null, ShipmentStatus.Failed, "Invalid response from courier.");
        }

        return (false, null, null, ShipmentStatus.Failed, "Unexpected courier response format.");
    }

    private static (bool Success, string? Awb, string? OrderNumber, ShipmentStatus Status, string? Error) ParseManifestItem(JsonElement item, string raw)
    {
        var successFlag = TryGetBool(item, "success");
        if (successFlag is null && TryGetString(item, "status") is { } statusString)
        {
            if (statusString.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                successFlag = true;
            }
            else if (statusString.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                     statusString.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                successFlag = false;
            }
        }

        var awb = TryGetString(item, "awb")
            ?? TryGetString(item, "awbNumber")
            ?? TryGetString(item, "AWB");

        var orderNumber = TryGetString(item, "orderNumber") ?? TryGetString(item, "order_number");

        var message = TryGetString(item, "message") ?? TryGetString(item, "error");

        if (successFlag == false || (awb is null && message is not null))
        {
            return (false, awb, orderNumber, ShipmentStatus.Failed, message ?? "Courier rejected manifest request.");
        }

        if (awb is not null || successFlag == true)
        {
            return (true, awb, orderNumber, ShipmentStatus.Created, null);
        }

        return (false, null, orderNumber, ShipmentStatus.Failed, message ?? "Manifest response missing AWB.");
    }

    public static (bool Success, ShipmentStatus Status, string? StatusText, string? Error) ParseTracking(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var statusText = FindStatusText(root);
            if (statusText is null && root.TryGetProperty("data", out var data))
            {
                statusText = FindStatusText(data);
            }

            if (statusText is null)
            {
                return (false, ShipmentStatus.InTransit, null, "Tracking response did not include status.");
            }

            return (true, UrbaneBoltStatusMapper.Map(statusText), statusText, null);
        }
        catch (JsonException)
        {
            return (false, ShipmentStatus.InTransit, null, "Invalid tracking response from courier.");
        }
    }

    public static (bool Success, ShipmentStatus Status, string? Error) ParseCancel(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var success = TryGetBool(root, "success")
                ?? (TryGetString(root, "status")?.Equals("success", StringComparison.OrdinalIgnoreCase) == true);

            if (success == true)
            {
                return (true, ShipmentStatus.Cancelled, null);
            }

            var message = TryGetString(root, "message") ?? TryGetString(root, "error") ?? "Cancellation failed.";
            return (false, ShipmentStatus.Failed, message);
        }
        catch (JsonException)
        {
            return (false, ShipmentStatus.Failed, "Invalid cancellation response from courier.");
        }
    }

    private static string? FindStatusText(JsonElement element)
    {
        var direct = TryGetString(element, "status")
            ?? TryGetString(element, "currentStatus")
            ?? TryGetString(element, "shipmentStatus");

        if (direct is not null)
        {
            return direct;
        }

        if (element.TryGetProperty("tracking", out var tracking) && tracking.ValueKind == JsonValueKind.Array && tracking.GetArrayLength() > 0)
        {
            var last = tracking[tracking.GetArrayLength() - 1];
            return TryGetString(last, "status") ?? TryGetString(last, "remarks");
        }

        if (element.TryGetProperty("history", out var history) && history.ValueKind == JsonValueKind.Array && history.GetArrayLength() > 0)
        {
            var last = history[history.GetArrayLength() - 1];
            return TryGetString(last, "status") ?? TryGetString(last, "remarks");
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static bool? TryGetBool(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(prop.GetString(), out var b) => b,
            _ => null
        };
    }
}
