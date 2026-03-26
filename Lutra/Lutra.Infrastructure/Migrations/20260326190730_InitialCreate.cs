using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Supermarkten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supermarkten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Verspaketten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AantalPersonen = table.Column<int>(type: "integer", nullable: false),
                    SupermarktId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verspaketten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verspaketten_Supermarkten_SupermarktId",
                        column: x => x.SupermarktId,
                        principalTable: "Supermarkten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beoordelingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CijferSmaak = table.Column<int>(type: "integer", nullable: false),
                    CijferBereiden = table.Column<int>(type: "integer", nullable: false),
                    Aanbevolen = table.Column<bool>(type: "boolean", nullable: false),
                    Tekst = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    VerspakketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beoordelingen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beoordelingen_Verspaketten_VerspakketId",
                        column: x => x.VerspakketId,
                        principalTable: "Verspaketten",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beoordelingen_VerspakketId",
                table: "Beoordelingen",
                column: "VerspakketId");

            migrationBuilder.CreateIndex(
                name: "IX_Verspaketten_SupermarktId",
                table: "Verspaketten",
                column: "SupermarktId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beoordelingen");

            migrationBuilder.DropTable(
                name: "Verspaketten");

            migrationBuilder.DropTable(
                name: "Supermarkten");
        }
    }
}
