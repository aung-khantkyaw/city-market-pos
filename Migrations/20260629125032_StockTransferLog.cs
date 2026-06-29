using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class StockTransferLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoreQuantity",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "CurrentQuantity",
                table: "GRNDetails",
                newName: "StoreQuantity");

            migrationBuilder.AddColumn<int>(
                name: "CurrentStockQuantity",
                table: "GRNDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStoreQuantity",
                table: "GRNDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "GRNDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "GRNDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "GRNDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "GRNDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "GRNDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StockTransferLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GRNDetailId = table.Column<int>(type: "int", nullable: false),
                    TransferQuantity = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_GRNDetails_GRNDetailId",
                        column: x => x.GRNDetailId,
                        principalTable: "GRNDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_GRNDetailId",
                table: "StockTransferLogs",
                column: "GRNDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferLogs");

            migrationBuilder.DropColumn(
                name: "CurrentStockQuantity",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "CurrentStoreQuantity",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "GRNDetails");

            migrationBuilder.RenameColumn(
                name: "StoreQuantity",
                table: "GRNDetails",
                newName: "CurrentQuantity");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoreQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
