using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(4000);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.OriginalPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(e => e.Slug)
            .IsUnique();

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.HasSizes)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.SizeType)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired(false);

        // 1:many Product → Reviews
        // Product has private backing field _reviews — EF finds it by naming convention
        builder.HasMany(e => e.Reviews)
            .WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
