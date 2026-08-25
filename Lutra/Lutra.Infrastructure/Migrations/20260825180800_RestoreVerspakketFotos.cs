using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RestoreVerspakketFotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerspakketFotos_VerspakketId",
                table: "VerspakketFotos");

            migrationBuilder.AddColumn<bool>(
                name: "IsMainImage",
                table: "VerspakketFotos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"VerspakketFotos\" SET \"IsMainImage\" = true;");

            migrationBuilder.CreateIndex(
                name: "IX_VerspakketFotos_VerspakketId",
                table: "VerspakketFotos",
                column: "VerspakketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerspakketFotos_VerspakketId",
                table: "VerspakketFotos");

            migrationBuilder.DropColumn(
                name: "IsMainImage",
                table: "VerspakketFotos");

            migrationBuilder.CreateIndex(
                name: "IX_VerspakketFotos_VerspakketId",
                table: "VerspakketFotos",
                column: "VerspakketId",
                unique: true);
        }
    }
}
