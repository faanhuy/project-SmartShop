using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class PriceCampaignConfiguration : IEntityTypeConfiguration<PriceCampaign>
{
    public void Configure(EntityTypeBuilder<PriceCampaign> builder)
    {
        builder.ToTable("PriceLists");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.AppliesToAll).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.StartsAt);
        builder.HasIndex(x => new { x.EndsAt, x.IsActive });

        // Ignore in-memory _storeIds collection — managed via PriceListStores join table in Repository
        builder.Ignore(x => x.StoreIds);

        // Items navigation
        builder.HasMany(x => x.ItemsNav)
            .WithOne()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
