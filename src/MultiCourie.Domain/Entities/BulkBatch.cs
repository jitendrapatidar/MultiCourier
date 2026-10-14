using MultiCourie.Domain.Enums;

namespace MultiCourie.Domain.Entities;

public class BulkBatch
{
    public Guid Id { get; set; }

    public BulkBatchStatus Status { get; set; } = BulkBatchStatus.Queued;

    public int TotalCount { get; set; }

    public int SucceededCount { get; set; }

    public int FailedCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<BulkBatchItem> Items { get; set; } = new List<BulkBatchItem>();
}
