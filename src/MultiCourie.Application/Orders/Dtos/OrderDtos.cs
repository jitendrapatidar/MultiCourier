using MultiCourie.Domain.Enums;

namespace MultiCourie.Application.Orders.Dtos;

public sealed class CreateOrderRequestDto
{
    public required string OrderId { get; init; }

    public required string CourierPartner { get; init; }

    public string? CustomerCode { get; init; }

    public required string ServiceType { get; init; }

    public required string PayMode { get; init; }

    public decimal DeclaredValue { get; init; }

    public decimal CollectableValue { get; init; }

    public decimal WeightKg { get; init; }

    public decimal LengthCm { get; init; }

    public decimal BreadthCm { get; init; }

    public decimal HeightCm { get; init; }

    public int Pieces { get; init; } = 1;

    public required PartyAddressDto Consignee { get; init; }

    public required PartyAddressDto Shipper { get; init; }

    public required PartyAddressDto ReturnTo { get; init; }

    public required InvoiceDetailsDto Invoice { get; init; }

    public required ItemDetailsDto Item { get; init; }
}

public sealed class PartyAddressDto
{
    public required string Name { get; init; }

    public required string Address { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string Pincode { get; init; }

    public required string Mobile { get; init; }

    public string? Email { get; init; }

    public string Country { get; init; } = "INDIA";

    public string AddressType { get; init; } = "Home";
}

public sealed class InvoiceDetailsDto
{
    public required string InvoiceNumber { get; init; }

    public required string InvoiceDate { get; init; }

    public decimal InvoiceValue { get; init; }
}

public sealed class ItemDetailsDto
{
    public required string Description { get; init; }

    public int Quantity { get; init; } = 1;
}

public sealed class OrderResponseDto
{
    public required string OrderId { get; init; }

    public required string CourierPartner { get; init; }

    public string? CourierOrderId { get; init; }

    public string? AwbNumber { get; init; }

    public required string Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public bool IdempotentReplay { get; init; }
}

public sealed class TrackOrderResponseDto
{
    public required string OrderId { get; init; }

    public required string Status { get; init; }

    public string? AwbNumber { get; init; }

    public string? CourierStatusText { get; init; }

    public IReadOnlyList<TrackingEventDto> History { get; init; } = Array.Empty<TrackingEventDto>();
}

public sealed class TrackingEventDto
{
    public required string Status { get; init; }

    public string? CourierStatusText { get; init; }

    public DateTimeOffset RecordedAt { get; init; }
}

public sealed class CancelOrderRequestDto
{
    public required string CourierPartner { get; init; }
}

public sealed class BulkCreateOrdersRequestDto
{
    public required IReadOnlyList<CreateOrderRequestDto> Orders { get; init; }
}

public sealed class BulkCreateAcceptedResponseDto
{
    public required Guid BatchId { get; init; }

    public required string Status { get; init; }

    public int TotalOrders { get; init; }
}

public sealed class BulkBatchStatusResponseDto
{
    public required Guid BatchId { get; init; }

    public required string Status { get; init; }

    public int TotalOrders { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<BulkOrderResultDto> Results { get; init; } = Array.Empty<BulkOrderResultDto>();
}

public sealed class BulkOrderResultDto
{
    public required string OrderId { get; init; }

    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public OrderResponseDto? Order { get; init; }
}

public static class ShipmentStatusMapper
{
    public static string ToApiString(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Pending => "PENDING",
        ShipmentStatus.Created => "CREATED",
        ShipmentStatus.PickedUp => "PICKED_UP",
        ShipmentStatus.InTransit => "IN_TRANSIT",
        ShipmentStatus.Delivered => "DELIVERED",
        ShipmentStatus.Cancelled => "CANCELLED",
        ShipmentStatus.Failed => "FAILED",
        _ => status.ToString().ToUpperInvariant()
    };
}
