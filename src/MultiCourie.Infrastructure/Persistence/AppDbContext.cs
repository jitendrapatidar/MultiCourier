using Microsoft.EntityFrameworkCore;
using MultiCourie.Domain.Entities;

namespace MultiCourie.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShipmentOrder> ShipmentOrders => Set<ShipmentOrder>();

    public DbSet<TrackingHistoryEntry> TrackingHistory => Set<TrackingHistoryEntry>();

    public DbSet<BulkBatch> BulkBatches => Set<BulkBatch>();

    public DbSet<BulkBatchItem> BulkBatchItems => Set<BulkBatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentOrder>(entity =>
        {
            entity.ToTable("shipment_orders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.Property(x => x.OrderId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CourierPartner).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CourierOrderId).HasMaxLength(200);
            entity.Property(x => x.AwbNumber).HasMaxLength(100);
            entity.Property(x => x.LastErrorCode).HasMaxLength(100);
            entity.Property(x => x.LastErrorMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<TrackingHistoryEntry>(entity =>
        {
            entity.ToTable("tracking_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ShipmentOrderId);
            entity.Property(x => x.CourierStatusText).HasMaxLength(500);
            entity.Property(x => x.RawPayload).IsRequired();
            entity.HasOne(x => x.ShipmentOrder)
                .WithMany(x => x.TrackingHistory)
                .HasForeignKey(x => x.ShipmentOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BulkBatch>(entity =>
        {
            entity.ToTable("bulk_batches");
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<BulkBatchItem>(entity =>
        {
            entity.ToTable("bulk_batch_items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.BulkBatchId, x.OrderId }).IsUnique();
            entity.Property(x => x.OrderId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.BulkBatch)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.BulkBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
