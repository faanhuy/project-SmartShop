using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class ProductEmbeddingConfiguration : IEntityTypeConfiguration<ProductEmbedding>
{
    public void Configure(EntityTypeBuilder<ProductEmbedding> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.EmbeddingJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.GeneratedAt)
            .IsRequired();

        builder.HasIndex(e => e.ProductId)
            .IsUnique();

        builder.HasOne(e => e.Product)
            .WithOne()
            .HasForeignKey<ProductEmbedding>(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
