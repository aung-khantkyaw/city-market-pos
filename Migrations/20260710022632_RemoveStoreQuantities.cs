using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStoreQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferLogs");

            migrationBuilder.DropColumn(
                name: "CurrentStoreQuantity",
                table: "GRNDetails");

            migrationBuilder.DropColumn(
                name: "StoreQuantity",
                table: "GRNDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStoreQuantity",
                table: "GRNDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoreQuantity",
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
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferQuantity = table.Column<int>(type: "int", nullable: false)
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
    }
}
