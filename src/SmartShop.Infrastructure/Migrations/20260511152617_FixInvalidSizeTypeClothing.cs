using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixInvalidSizeTypeClothing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 'Clothing' was an old enum value that no longer exists in SizeType.
            // Null it out so EF Core can deserialize products without throwing.
            migrationBuilder.Sql(
                "UPDATE Products SET SizeType = NULL WHERE SizeType = 'Clothing'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
