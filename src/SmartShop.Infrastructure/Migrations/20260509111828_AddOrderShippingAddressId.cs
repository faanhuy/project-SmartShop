using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingAddressId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingProvinceName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingWardName",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "ShippingAddressId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingProvinceId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingWardId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingProvinceId",
                table: "Orders",
                column: "ShippingProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingWardId",
                table: "Orders",
                column: "ShippingWardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Provinces_ShippingProvinceId",
                table: "Orders",
                column: "ShippingProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Wards_ShippingWardId",
                table: "Orders",
                column: "ShippingWardId",
                principalTable: "Wards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Provinces_ShippingProvinceId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Wards_ShippingWardId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingProvinceId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingWardId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProvinceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingWardId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "ShippingProvinceName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingWardName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
