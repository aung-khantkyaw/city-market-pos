using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceFromPODetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PurchaseOrderDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "PurchaseOrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseOrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
