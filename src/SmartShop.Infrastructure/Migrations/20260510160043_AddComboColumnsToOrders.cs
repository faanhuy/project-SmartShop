using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComboColumnsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ComboDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ComboPromotionId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComboPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TriggerProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerSizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerMinQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    RewardProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RewardSizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RewardQuantity = table.Column<int>(type: "int", nullable: true),
                    RewardAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboPromotions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComboPromotions_IsActive_StartsAt_EndsAt",
                table: "ComboPromotions",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComboPromotions");

            migrationBuilder.DropColumn(
                name: "ComboDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ComboPromotionId",
                table: "Orders");
        }
    }
}
