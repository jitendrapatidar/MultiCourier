using MultiCourie.Application.Orders.Dtos;

namespace MultiCourie.Application.Orders;

public interface IBulkOrderQueue
{
    ValueTask EnqueueAsync(BulkOrderWorkItem workItem, CancellationToken cancellationToken = default);
}

public sealed record BulkOrderWorkItem(Guid BatchId, IReadOnlyList<CreateOrderRequestDto> Orders);
