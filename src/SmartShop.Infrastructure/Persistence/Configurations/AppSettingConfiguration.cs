using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");
        builder.HasKey(x => x.Key);

        builder.Property(x => x.Key)        .HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value)       .HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DataType)    .HasMaxLength(20).IsRequired().HasDefaultValue("text");
        builder.Property(x => x.Description) .HasMaxLength(500);
        builder.Property(x => x.UpdatedAt)   .IsRequired();

    }
}
