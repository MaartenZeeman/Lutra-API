using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddPrijsInCentenToVerspakket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrijsInCenten",
                table: "Verspaketten",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrijsInCenten",
                table: "Verspaketten");
        }
    }
}
