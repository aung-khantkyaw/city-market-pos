using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddGroundBalanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroundBalances",
                columns: table => new
                {
                    BatchNo = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    GBDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundBalances", x => x.BatchNo);
                });

            migrationBuilder.CreateTable(
                name: "GroundBalanceDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNo = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ActualQty = table.Column<float>(type: "real", nullable: false),
                    InventoryQty = table.Column<float>(type: "real", nullable: false),
                    DifferentQty = table.Column<float>(type: "real", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundBalanceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroundBalanceDetails_GroundBalances_BatchNo",
                        column: x => x.BatchNo,
                        principalTable: "GroundBalances",
                        principalColumn: "BatchNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroundBalanceDetails_BatchNo",
                table: "GroundBalanceDetails",
                column: "BatchNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroundBalanceDetails");

            migrationBuilder.DropTable(
                name: "GroundBalances");
        }
    }
}
