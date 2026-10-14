using MultiCourie.Domain.Entities;

namespace MultiCourie.Application.Abstractions.Persistence;

public interface IShipmentOrderRepository
{
    Task<ShipmentOrder?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);

    Task<ShipmentOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ShipmentOrder order, CancellationToken cancellationToken = default);

    Task AddTrackingEntryAsync(TrackingHistoryEntry entry, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
