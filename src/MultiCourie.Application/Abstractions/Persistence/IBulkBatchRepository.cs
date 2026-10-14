using MultiCourie.Domain.Entities;

namespace MultiCourie.Application.Abstractions.Persistence;

public interface IBulkBatchRepository
{
    Task AddAsync(BulkBatch batch, CancellationToken cancellationToken = default);

    Task<BulkBatch?> GetByIdAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
