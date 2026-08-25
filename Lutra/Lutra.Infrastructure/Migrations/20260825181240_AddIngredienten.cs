using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredienten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredienten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Hoeveelheid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Eenheid = table.Column<int>(type: "integer", nullable: false),
                    Inbegrepen = table.Column<bool>(type: "boolean", nullable: false),
                    VerspakketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredienten", x => x.Id);
                    table.CheckConstraint("CK_Ingredienten_Hoeveelheid", "\"Hoeveelheid\" > 0");
                    table.ForeignKey(
                        name: "FK_Ingredienten_Verspaketten_VerspakketId",
                        column: x => x.VerspakketId,
                        principalTable: "Verspaketten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredienten_VerspakketId",
                table: "Ingredienten",
                column: "VerspakketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ingredienten");
        }
    }
}
