using Microsoft.EntityFrameworkCore;
using MultiCourie.Application.Abstractions.Persistence;
using MultiCourie.Domain.Entities;

namespace MultiCourie.Infrastructure.Persistence;

public sealed class BulkBatchRepository : IBulkBatchRepository
{
    private readonly AppDbContext _db;

    public BulkBatchRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(BulkBatch batch, CancellationToken cancellationToken = default) =>
        await _db.BulkBatches.AddAsync(batch, cancellationToken);

    public Task<BulkBatch?> GetByIdAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        _db.BulkBatches
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == batchId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
