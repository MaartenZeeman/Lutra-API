using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddVoedingswaardeBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voedingswaarden_VerspakketId",
                table: "Voedingswaarden");

            migrationBuilder.AddColumn<int>(
                name: "Basis",
                table: "Voedingswaarden",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Voedingswaarden_VerspakketId_Basis",
                table: "Voedingswaarden",
                columns: new[] { "VerspakketId", "Basis" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voedingswaarden_VerspakketId_Basis",
                table: "Voedingswaarden");

            migrationBuilder.DropColumn(
                name: "Basis",
                table: "Voedingswaarden");

            migrationBuilder.CreateIndex(
                name: "IX_Voedingswaarden_VerspakketId",
                table: "Voedingswaarden",
                column: "VerspakketId",
                unique: true);
        }
    }
}
