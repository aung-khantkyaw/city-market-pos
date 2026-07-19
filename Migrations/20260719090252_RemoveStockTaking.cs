using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStockTaking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTakingDetails");

            migrationBuilder.DropTable(
                name: "StockTakings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTakings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConductedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConductedByUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockTakingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TakingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTakingDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GRNDetailId = table.Column<int>(type: "int", nullable: false),
                    StockTakingId = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<int>(type: "int", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Variance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakingDetails_GRNDetails_GRNDetailId",
                        column: x => x.GRNDetailId,
                        principalTable: "GRNDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTakingDetails_StockTakings_StockTakingId",
                        column: x => x.StockTakingId,
                        principalTable: "StockTakings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingDetails_GRNDetailId",
                table: "StockTakingDetails",
                column: "GRNDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingDetails_StockTakingId",
                table: "StockTakingDetails",
                column: "StockTakingId");
        }
    }
}
