using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shadow join table PriceListStores — no .NET entity class needed.
/// Mapped via Dictionary&lt;string, object&gt; (EF shared-type entity).
/// </summary>
public class PriceListStoreConfiguration : IEntityTypeConfiguration<PriceListStoreRow>
{
    public void Configure(EntityTypeBuilder<PriceListStoreRow> builder)
    {
        builder.ToTable("PriceListStores");
        builder.HasKey(x => new { x.CampaignId, x.StoreId });

        builder.Property(x => x.CampaignId).IsRequired();
        builder.Property(x => x.StoreId).IsRequired();
    }
}

/// <summary>
/// Lightweight join row — Infrastructure only, not exposed to Domain.
/// </summary>
public class PriceListStoreRow
{
    public Guid CampaignId { get; set; }
    public Guid StoreId { get; set; }
}
