using MultiCourie.Domain.Enums;

namespace MultiCourie.Domain.Entities;

public class ShipmentOrder
{
    public Guid Id { get; set; }

    /// <summary>Caller-provided idempotency key.</summary>
    public required string OrderId { get; set; }

    public required string CourierPartner { get; set; }

    public string? CourierOrderId { get; set; }

    public string? AwbNumber { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

    public string? CourierRequestPayload { get; set; }

    public string? CourierResponsePayload { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TrackingHistoryEntry> TrackingHistory { get; set; } = new List<TrackingHistoryEntry>();
}
