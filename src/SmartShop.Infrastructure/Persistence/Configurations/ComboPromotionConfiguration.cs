using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class ComboPromotionConfiguration : IEntityTypeConfiguration<ComboPromotion>
{
    public void Configure(EntityTypeBuilder<ComboPromotion> builder)
    {
        builder.ToTable("ComboPromotions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RewardAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TriggerMinQuantity)
            .HasDefaultValue(1);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.IsActive, x.StartsAt, x.EndsAt });
    }
}
