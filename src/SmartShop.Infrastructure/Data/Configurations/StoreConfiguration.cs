using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Structured geography FKs (Sprint 18B)
        builder.Property(s => s.Street).HasMaxLength(500).IsRequired(false);
        builder.Property(s => s.ProvinceId).IsRequired(false);
        builder.Property(s => s.WardId).IsRequired(false);

        builder.HasOne(s => s.Province)
            .WithMany()
            .HasForeignKey(s => s.ProvinceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Ward)
            .WithMany()
            .HasForeignKey(s => s.WardId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
