using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMarketPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryShortName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "Categories",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ShortName",
                table: "Categories",
                column: "ShortName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_ShortName",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "Categories");
        }
    }
}
