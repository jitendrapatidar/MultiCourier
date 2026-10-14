using MultiCourie.Domain.Enums;

namespace MultiCourie.Application.Orders.Models;

public sealed class CourierCreateResult
{
    public bool Success { get; init; }

    public string? CourierOrderId { get; init; }

    public string? AwbNumber { get; init; }

    public ShipmentStatus Status { get; init; }

    public required string RequestPayload { get; init; }

    public required string ResponsePayload { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class CourierTrackResult
{
    public bool Success { get; init; }

    public ShipmentStatus Status { get; init; }

    public string? CourierStatusText { get; init; }

    public required string ResponsePayload { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class CourierCancelResult
{
    public bool Success { get; init; }

    public ShipmentStatus Status { get; init; }

    public required string RequestPayload { get; init; }

    public required string ResponsePayload { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}
