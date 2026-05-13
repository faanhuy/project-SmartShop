using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(e => e.Id);

        // Snapshot of product name at order time — not a FK to Product.Name
        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.SizeLabel)
            .HasMaxLength(20);

        builder.Property(e => e.OriginalUnitPrice)
            .HasPrecision(18, 2);

        // SubTotal is a C# computed property (UnitPrice * Quantity) — not a DB column
        builder.Ignore(e => e.SubTotal);

        // many:1 OrderItem → Product (restrict: don't delete product referenced by orders)
        // Order → Items relationship is configured in OrderConfiguration
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
