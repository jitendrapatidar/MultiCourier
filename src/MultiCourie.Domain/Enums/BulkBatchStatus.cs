namespace MultiCourie.Domain.Enums;

public enum BulkBatchStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    PartiallyCompleted = 3,
    Failed = 4
}
