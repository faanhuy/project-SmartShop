using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class StoreInventoryConfiguration : IEntityTypeConfiguration<StoreInventory>
{
    public void Configure(EntityTypeBuilder<StoreInventory> builder)
    {
        builder.ToTable("StoreInventories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LowStockThreshold)
            .IsRequired()
            .HasDefaultValue(5);

        // Optimistic concurrency token
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Unique composite index on (StoreId, ProductId)
        builder.HasIndex(e => new { e.StoreId, e.ProductId })
            .IsUnique();

        // FK: StoreId → Stores (no cascade delete)
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: ProductId → Products (no cascade delete)
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
