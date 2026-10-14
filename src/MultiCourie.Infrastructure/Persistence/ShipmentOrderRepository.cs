using Microsoft.EntityFrameworkCore;
using MultiCourie.Application.Abstractions.Persistence;
using MultiCourie.Domain.Entities;

namespace MultiCourie.Infrastructure.Persistence;

public sealed class ShipmentOrderRepository : IShipmentOrderRepository
{
    private readonly AppDbContext _db;

    public ShipmentOrderRepository(AppDbContext db) => _db = db;

    public Task<ShipmentOrder?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default) =>
        _db.ShipmentOrders
            .Include(x => x.TrackingHistory)
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public Task<ShipmentOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ShipmentOrders
            .Include(x => x.TrackingHistory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ShipmentOrder order, CancellationToken cancellationToken = default) =>
        await _db.ShipmentOrders.AddAsync(order, cancellationToken);

    public async Task AddTrackingEntryAsync(TrackingHistoryEntry entry, CancellationToken cancellationToken = default) =>
        await _db.TrackingHistory.AddAsync(entry, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
