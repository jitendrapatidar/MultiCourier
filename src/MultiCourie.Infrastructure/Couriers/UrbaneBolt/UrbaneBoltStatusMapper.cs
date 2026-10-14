using MultiCourie.Domain.Enums;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
///     Provides a mapping from Urbane Bolt status text to the internal ShipmentStatus enum used in the application.
/// </summary>
internal static class UrbaneBoltStatusMapper
{
    public static ShipmentStatus Map(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            return ShipmentStatus.InTransit;
        }

        var normalized = statusText.Trim().ToUpperInvariant();

        if (normalized.Contains("DELIVER", StringComparison.Ordinal))
        {
            return ShipmentStatus.Delivered;
        }

        if (normalized.Contains("CANCEL", StringComparison.Ordinal))
        {
            return ShipmentStatus.Cancelled;
        }

        if (normalized.Contains("PICK", StringComparison.Ordinal) || normalized.Contains("OFD", StringComparison.Ordinal))
        {
            return ShipmentStatus.PickedUp;
        }

        if (normalized.Contains("FAIL", StringComparison.Ordinal) || normalized.Contains("UNDEL", StringComparison.Ordinal))
        {
            return ShipmentStatus.Failed;
        }

        if (normalized.Contains("MANIFEST", StringComparison.Ordinal) || normalized.Contains("BOOK", StringComparison.Ordinal))
        {
            return ShipmentStatus.Created;
        }

        return ShipmentStatus.InTransit;
    }
}
