using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddPrijsInCentenCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Verspaketten_PrijsInCenten",
                table: "Verspaketten",
                sql: "\"PrijsInCenten\" IS NULL OR \"PrijsInCenten\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Verspaketten_PrijsInCenten",
                table: "Verspaketten");
        }
    }
}
