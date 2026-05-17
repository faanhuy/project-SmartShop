using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class ComboConfiguration : IEntityTypeConfiguration<Combo>
{
    public void Configure(EntityTypeBuilder<Combo> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.Description)
            .HasMaxLength(4000);

        builder.Property(e => e.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.OriginalPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.StartsAt)
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .IsRequired(false);

        // 1:many Combo → ComboItems
        builder.HasMany(e => e.Items)
            .WithOne(i => i.Combo)
            .HasForeignKey(i => i.ComboId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for active combos query
        builder.HasIndex(e => new { e.IsActive, e.StartsAt, e.EndsAt })
            .HasDatabaseName("IX_Combo_Active");
    }
}
