using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddVerspakketBronUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BronUrl",
                table: "Verspaketten",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verspaketten_BronUrl",
                table: "Verspaketten",
                column: "BronUrl",
                unique: true,
                filter: "\"BronUrl\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Verspaketten_BronUrl",
                table: "Verspaketten");

            migrationBuilder.DropColumn(
                name: "BronUrl",
                table: "Verspaketten");
        }
    }
}
