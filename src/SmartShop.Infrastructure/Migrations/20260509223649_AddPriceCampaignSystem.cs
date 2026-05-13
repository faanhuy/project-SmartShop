using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCampaignSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliesToAll = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceListStores",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListStores", x => new { x.CampaignId, x.StoreId });
                });

            migrationBuilder.CreateTable(
                name: "PriceCampaignItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceCampaignItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceCampaignItems_PriceLists_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceCampaignItems_CampaignId_ProductId",
                table: "PriceCampaignItems",
                columns: new[] { "CampaignId", "ProductId" },
                unique: true,
                filter: "[SizeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PriceCampaignItems_CampaignId_ProductId_SizeId",
                table: "PriceCampaignItems",
                columns: new[] { "CampaignId", "ProductId", "SizeId" },
                unique: true,
                filter: "[SizeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_EndsAt_IsActive",
                table: "PriceLists",
                columns: new[] { "EndsAt", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_StartsAt",
                table: "PriceLists",
                column: "StartsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceCampaignItems");

            migrationBuilder.DropTable(
                name: "PriceListStores");

            migrationBuilder.DropTable(
                name: "PriceLists");
        }
    }
}
