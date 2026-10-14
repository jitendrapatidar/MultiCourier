namespace MultiCourie.Domain.Entities;

public class BulkBatchItem
{
    public Guid Id { get; set; }

    public Guid BulkBatchId { get; set; }

    public BulkBatch BulkBatch { get; set; } = null!;

    public required string OrderId { get; set; }

    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public Guid? ShipmentOrderId { get; set; }
}
