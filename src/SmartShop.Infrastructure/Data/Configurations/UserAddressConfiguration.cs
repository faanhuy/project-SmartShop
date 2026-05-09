using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Configurations;

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.RecipientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Street)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Ward)
            .HasMaxLength(200);

        builder.Property(a => a.District)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_UserAddresses_UserId");

        // Structured geography FKs (Sprint 18B)
        builder.Property(a => a.ProvinceId).IsRequired(false);
        builder.Property(a => a.WardId).IsRequired(false);

        builder.HasOne(a => a.Province)
            .WithMany()
            .HasForeignKey(a => a.ProvinceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.WardEntity)
            .WithMany()
            .HasForeignKey(a => a.WardId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
