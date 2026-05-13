using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class PriceCampaignItemConfiguration : IEntityTypeConfiguration<PriceCampaignItem>
{
    public void Configure(EntityTypeBuilder<PriceCampaignItem> builder)
    {
        builder.ToTable("PriceCampaignItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscountValue)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.RuleType)
            .IsRequired();

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.CampaignId).IsRequired();
        builder.Property(x => x.SizeId).IsRequired(false);

        // Filtered unique indexes per schema spec
        builder.HasIndex(x => new { x.CampaignId, x.ProductId })
            .HasFilter("[SizeId] IS NULL")
            .IsUnique();

        builder.HasIndex(x => new { x.CampaignId, x.ProductId, x.SizeId })
            .HasFilter("[SizeId] IS NOT NULL")
            .IsUnique();
    }
}
