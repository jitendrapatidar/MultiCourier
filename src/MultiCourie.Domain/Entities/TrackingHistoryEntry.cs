using MultiCourie.Domain.Enums;

namespace MultiCourie.Domain.Entities;

public class TrackingHistoryEntry
{
    public Guid Id { get; set; }

    public Guid ShipmentOrderId { get; set; }

    public ShipmentOrder ShipmentOrder { get; set; } = null!;

    public ShipmentStatus Status { get; set; }

    public string? CourierStatusText { get; set; }

    public required string RawPayload { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
