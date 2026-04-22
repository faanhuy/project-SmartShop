using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Coupons]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Coupons] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [DiscountType] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [MaxUsage] int NOT NULL,
        [UsedQuantity] int NOT NULL,
        [MinOrderValue] decimal(18,2) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL
    )
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Coupons_Code')
BEGIN
    CREATE UNIQUE INDEX [IX_Coupons_Code] ON [dbo].[Coupons] ([Code])
END

");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CouponUsages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CouponUsages] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [UserId] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [CouponId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL
    )
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CouponUsages_CouponId_UserId')
BEGIN
    CREATE UNIQUE INDEX [IX_CouponUsages_CouponId_UserId] ON [dbo].[CouponUsages] ([CouponId], [UserId])
END
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CouponUsages_OrderId')
BEGIN
    CREATE INDEX [IX_CouponUsages_OrderId] ON [dbo].[CouponUsages] ([OrderId])
END
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CouponUsages_UserId')
BEGIN
    CREATE INDEX [IX_CouponUsages_UserId] ON [dbo].[CouponUsages] ([UserId])
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CouponUsages");

            migrationBuilder.DropTable(
                name: "Coupons");
        }
    }
}
