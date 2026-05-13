using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SizeLabel)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => new { x.ProductId, x.SizeLabel })
            .IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Sizes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Size)
            .WithMany()
            .HasForeignKey(x => x.SizeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
