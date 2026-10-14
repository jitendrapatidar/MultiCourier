using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiCourie.Application.Abstractions.Persistence;
using MultiCourie.Application.Common;
using MultiCourie.Application.Orders;
using MultiCourie.Application.Orders.Dtos;
using MultiCourie.Domain.Enums;
using MultiCourie.Infrastructure.Options;

namespace MultiCourie.Infrastructure.Background;
/// <summary>
/// Background service that processes bulk order batches from a channel queue.
/// </summary>
public sealed class BulkBatchProcessingService : BackgroundService
{
    private readonly BulkOrderChannelQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BulkProcessingOptions _options;
    private readonly ILogger<BulkBatchProcessingService> _logger;
    /// <summary>
    ///     Initializes a new instance of the <see cref="BulkBatchProcessingService"/> class.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public BulkBatchProcessingService(
        BulkOrderChannelQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<BulkProcessingOptions> options,
        ILogger<BulkBatchProcessingService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }
    /// <summary>
    /// Processes bulk order work items from the queue asynchronously until the service is stopped.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessBatchAsync(work, stoppingToken);
        }
    }
    /// <summary>
    /// Processes a single bulk order batch, updating its status and processing each order concurrently with a limit on the maximum concurrency.
    /// </summary>
    /// <param name="work"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ProcessBatchAsync(BulkOrderWorkItem work, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var batches = scope.ServiceProvider.GetRequiredService<IBulkBatchRepository>();
        var batch = await batches.GetByIdAsync(work.BatchId, cancellationToken);
        if (batch is null)
        {
            _logger.LogWarning("Bulk batch {BatchId} not found", work.BatchId);
            return;
        }

        batch.Status = BulkBatchStatus.Processing;
        await batches.SaveChangesAsync(cancellationToken);

        var semaphore = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrency));
        var tasks = work.Orders.Select(async orderDto =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessSingleOrderAsync(work.BatchId, orderDto, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        await using var finalScope = _scopeFactory.CreateAsyncScope();
        var finalBatches = finalScope.ServiceProvider.GetRequiredService<IBulkBatchRepository>();
        var finalBatch = await finalBatches.GetByIdAsync(work.BatchId, cancellationToken);
        if (finalBatch is null)
        {
            return;
        }

        finalBatch.SucceededCount = finalBatch.Items.Count(i => i.Success);
        finalBatch.FailedCount = finalBatch.Items.Count(i => !i.Success);
        finalBatch.CompletedAt = DateTimeOffset.UtcNow;
        finalBatch.Status = finalBatch.FailedCount == 0
            ? BulkBatchStatus.Completed
            : finalBatch.SucceededCount == 0
                ? BulkBatchStatus.Failed
                : BulkBatchStatus.PartiallyCompleted;

        await finalBatches.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    ///     Processes a single order within a bulk batch, handling success and failure cases, and updating the corresponding batch item with the result.
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="orderDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ProcessSingleOrderAsync(Guid batchId, CreateOrderRequestDto orderDto, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var batches = scope.ServiceProvider.GetRequiredService<IBulkBatchRepository>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var orders = scope.ServiceProvider.GetRequiredService<IShipmentOrderRepository>();

        var batch = await batches.GetByIdAsync(batchId, cancellationToken);
        var item = batch?.Items.FirstOrDefault(i => i.OrderId == orderDto.OrderId);
        if (item is null)
        {
            return;
        }

        try
        {
            var result = await orderService.CreateOrderAsync(orderDto, cancellationToken);
            var persisted = await orders.GetByOrderIdAsync(orderDto.OrderId, cancellationToken);

            item.Success = true;
            item.ShipmentOrderId = persisted?.Id;
            item.ErrorCode = null;
            item.ErrorMessage = null;
        }
        catch (AppException ex)
        {
            item.Success = false;
            item.ErrorCode = ex.Code;
            item.ErrorMessage = ex.Message;
            _logger.LogWarning("Bulk item failed for order {OrderId}: {ErrorCode}", orderDto.OrderId, ex.Code);
        }
        catch (Exception ex)
        {
            item.Success = false;
            item.ErrorCode = ErrorCodes.CourierUnavailable;
            item.ErrorMessage = "Unexpected error while processing bulk order.";
            _logger.LogError(ex, "Bulk item unexpected failure for order {OrderId}", orderDto.OrderId);
        }

        await batches.SaveChangesAsync(cancellationToken);
    }
}
