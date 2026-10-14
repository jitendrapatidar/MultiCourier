using Microsoft.Extensions.Logging;
using MultiCourie.Application.Abstractions.Persistence;
using MultiCourie.Application.Common;
using MultiCourie.Application.Couriers;
using MultiCourie.Application.Orders.Dtos;
using MultiCourie.Application.Orders.Models;
using MultiCourie.Domain.Entities;
using MultiCourie.Domain.Enums;

namespace MultiCourie.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IShipmentOrderRepository _orders;
    private readonly IBulkBatchRepository _batches;
    private readonly CourierPartnerResolver _courierResolver;
    private readonly IBulkOrderQueue _bulkQueue;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IShipmentOrderRepository orders,
        IBulkBatchRepository batches,
        CourierPartnerResolver courierResolver,
        IBulkOrderQueue bulkQueue,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _batches = batches;
        _courierResolver = courierResolver;
        _bulkQueue = bulkQueue;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _orders.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return OrderMapping.ToResponse(existing, idempotentReplay: true);
        }

        var normalized = OrderMapping.ToNormalized(request);
        var partner = _courierResolver.Resolve(normalized.CourierPartner);

        var entity = new ShipmentOrder
        {
            Id = Guid.NewGuid(),
            OrderId = normalized.OrderId,
            CourierPartner = normalized.CourierPartner.ToLowerInvariant(),
            Status = ShipmentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _orders.AddAsync(entity, cancellationToken);
        await _orders.SaveChangesAsync(cancellationToken);

        CourierCreateResult result;
        try
        {
            result = await partner.CreateShipmentAsync(normalized, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier create failed for order {OrderId}, partner {CourierPartner}", request.OrderId, request.CourierPartner);
            entity.Status = ShipmentStatus.Failed;
            entity.LastErrorCode = ErrorCodes.CourierUnavailable;
            entity.LastErrorMessage = "Courier integration failed.";
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _orders.SaveChangesAsync(cancellationToken);
            throw new AppException(ErrorCodes.CourierUnavailable, "Unable to create shipment with courier at this time.", 502);
        }

        entity.CourierRequestPayload = result.RequestPayload;
        entity.CourierResponsePayload = result.ResponsePayload;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (result.Success)
        {
            entity.CourierOrderId = result.CourierOrderId ?? normalized.OrderId;
            entity.AwbNumber = result.AwbNumber;
            entity.Status = result.Status;
            await AppendTrackingAsync(entity, result.Status, null, result.ResponsePayload, cancellationToken);
        }
        else
        {
            entity.Status = ShipmentStatus.Failed;
            entity.LastErrorCode = result.ErrorCode ?? ErrorCodes.CourierClientError;
            entity.LastErrorMessage = result.ErrorMessage ?? "Courier rejected the shipment request.";
            throw new AppException(
                result.ErrorCode ?? ErrorCodes.CourierClientError,
                entity.LastErrorMessage,
                statusCode: 422);
        }

        await _orders.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToResponse(entity);
    }

    public async Task<TrackOrderResponseDto> TrackOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new AppException(ErrorCodes.OrderNotFound, $"Order '{orderId}' was not found.", 404);

        if (string.IsNullOrWhiteSpace(order.AwbNumber))
        {
            return new TrackOrderResponseDto
            {
                OrderId = order.OrderId,
                Status = ShipmentStatusMapper.ToApiString(order.Status),
                AwbNumber = order.AwbNumber,
                History = Array.Empty<TrackingEventDto>()
            };
        }

        var partner = _courierResolver.Resolve(order.CourierPartner);
        var track = await partner.TrackShipmentAsync(order.AwbNumber, cancellationToken);

        if (track.Success)
        {
            var last = order.TrackingHistory.OrderByDescending(h => h.RecordedAt).FirstOrDefault();
            var statusChanged = track.Status != order.Status;
            var shouldRecord = statusChanged ||
                               last is null ||
                               last.Status != track.Status ||
                               !string.Equals(last.CourierStatusText, track.CourierStatusText, StringComparison.Ordinal);

            if (statusChanged)
            {
                order.Status = track.Status;
                order.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (shouldRecord)
            {
                await AppendTrackingAsync(order, track.Status, track.CourierStatusText, track.ResponsePayload, cancellationToken);
            }

            if (statusChanged || shouldRecord)
            {
                await _orders.SaveChangesAsync(cancellationToken);
            }
        }

        var history = order.TrackingHistory
            .OrderByDescending(h => h.RecordedAt)
            .Select(h => new TrackingEventDto
            {
                Status = ShipmentStatusMapper.ToApiString(h.Status),
                CourierStatusText = h.CourierStatusText,
                RecordedAt = h.RecordedAt
            })
            .ToList();

        return new TrackOrderResponseDto
        {
            OrderId = order.OrderId,
            Status = ShipmentStatusMapper.ToApiString(order.Status),
            AwbNumber = order.AwbNumber,
            CourierStatusText = track.CourierStatusText,
            History = history
        };
    }

    public async Task<OrderResponseDto> CancelOrderAsync(string orderId, CancelOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new AppException(ErrorCodes.OrderNotFound, $"Order '{orderId}' was not found.", 404);

        if (!string.Equals(order.CourierPartner, request.CourierPartner, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(ErrorCodes.ValidationError, "courier_partner does not match the stored order.", 400);
        }

        if (order.Status is ShipmentStatus.Cancelled)
        {
            return OrderMapping.ToResponse(order, idempotentReplay: true);
        }

        if (string.IsNullOrWhiteSpace(order.AwbNumber))
        {
            throw new AppException(ErrorCodes.CancelNotAllowed, "Shipment has no AWB and cannot be cancelled.", 422);
        }

        var partner = _courierResolver.Resolve(order.CourierPartner);
        var cancel = await partner.CancelShipmentAsync(order.AwbNumber, cancellationToken);

        order.CourierRequestPayload = cancel.RequestPayload;
        order.CourierResponsePayload = cancel.ResponsePayload;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (cancel.Success)
        {
            order.Status = ShipmentStatus.Cancelled;
            await AppendTrackingAsync(order, ShipmentStatus.Cancelled, "Cancelled", cancel.ResponsePayload, cancellationToken);
        }
        else
        {
            order.LastErrorCode = cancel.ErrorCode ?? ErrorCodes.CourierClientError;
            order.LastErrorMessage = cancel.ErrorMessage ?? "Courier rejected cancellation.";
            await _orders.SaveChangesAsync(cancellationToken);
            throw new AppException(order.LastErrorCode, order.LastErrorMessage, 422);
        }

        await _orders.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToResponse(order);
    }

    public async Task<BulkCreateAcceptedResponseDto> EnqueueBulkCreateAsync(BulkCreateOrdersRequestDto request, CancellationToken cancellationToken = default)
    {
        var batch = new BulkBatch
        {
            Id = Guid.NewGuid(),
            Status = BulkBatchStatus.Queued,
            TotalCount = request.Orders.Count,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var order in request.Orders)
        {
            batch.Items.Add(new BulkBatchItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.OrderId,
                Success = false
            });
        }

        await _batches.AddAsync(batch, cancellationToken);
        await _batches.SaveChangesAsync(cancellationToken);

        await _bulkQueue.EnqueueAsync(new BulkOrderWorkItem(batch.Id, request.Orders), cancellationToken);

        return new BulkCreateAcceptedResponseDto
        {
            BatchId = batch.Id,
            Status = "PROCESSING",
            TotalOrders = batch.TotalCount
        };
    }

    public async Task<BulkBatchStatusResponseDto> GetBulkBatchStatusAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _batches.GetByIdAsync(batchId, cancellationToken)
            ?? throw new AppException(ErrorCodes.BatchNotFound, $"Batch '{batchId}' was not found.", 404);

        var results = new List<BulkOrderResultDto>();
        foreach (var item in batch.Items)
        {
            OrderResponseDto? orderDto = null;
            if (item.Success)
            {
                var order = await _orders.GetByOrderIdAsync(item.OrderId, cancellationToken);
                if (order is not null)
                {
                    orderDto = OrderMapping.ToResponse(order);
                }
            }

            results.Add(new BulkOrderResultDto
            {
                OrderId = item.OrderId,
                Success = item.Success,
                ErrorCode = item.ErrorCode,
                ErrorMessage = item.ErrorMessage,
                Order = orderDto
            });
        }

        return new BulkBatchStatusResponseDto
        {
            BatchId = batch.Id,
            Status = batch.Status.ToString().ToUpperInvariant(),
            TotalOrders = batch.TotalCount,
            Succeeded = batch.SucceededCount,
            Failed = batch.FailedCount,
            Results = results
        };
    }

    private async Task AppendTrackingAsync(
        ShipmentOrder order,
        ShipmentStatus status,
        string? courierStatusText,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        await _orders.AddTrackingEntryAsync(new TrackingHistoryEntry
        {
            Id = Guid.NewGuid(),
            ShipmentOrderId = order.Id,
            Status = status,
            CourierStatusText = courierStatusText,
            RawPayload = rawPayload,
            RecordedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
