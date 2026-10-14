using MultiCourie.Application.Orders.Dtos;

namespace MultiCourie.Application.Orders;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default);

    Task<TrackOrderResponseDto> TrackOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task<OrderResponseDto> CancelOrderAsync(string orderId, CancelOrderRequestDto request, CancellationToken cancellationToken = default);

    Task<BulkCreateAcceptedResponseDto> EnqueueBulkCreateAsync(BulkCreateOrdersRequestDto request, CancellationToken cancellationToken = default);

    Task<BulkBatchStatusResponseDto> GetBulkBatchStatusAsync(Guid batchId, CancellationToken cancellationToken = default);
}
