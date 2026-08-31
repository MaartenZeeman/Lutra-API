using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddVoedingswaardeEnAllergenen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerspakketAllergenen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Allergeen = table.Column<int>(type: "integer", nullable: false),
                    VerspakketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerspakketAllergenen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerspakketAllergenen_Verspaketten_VerspakketId",
                        column: x => x.VerspakketId,
                        principalTable: "Verspaketten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Voedingswaarden",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnergieKj = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EnergieKcal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Vetten = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WaarvanVerzadigd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Koolhydraten = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WaarvanSuikers = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Vezels = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Eiwitten = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Zout = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    VerspakketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voedingswaarden", x => x.Id);
                    table.CheckConstraint("CK_Voedingswaarden_Eiwitten", "\"Eiwitten\" IS NULL OR \"Eiwitten\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_EnergieKcal", "\"EnergieKcal\" IS NULL OR \"EnergieKcal\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_EnergieKj", "\"EnergieKj\" IS NULL OR \"EnergieKj\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_Koolhydraten", "\"Koolhydraten\" IS NULL OR \"Koolhydraten\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_Vetten", "\"Vetten\" IS NULL OR \"Vetten\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_Vezels", "\"Vezels\" IS NULL OR \"Vezels\" >= 0");
                    table.CheckConstraint("CK_Voedingswaarden_WaarvanSuikers", "\"WaarvanSuikers\" IS NULL OR CAST(\"Koolhydraten\" AS NUMERIC) IS NULL OR CAST(\"WaarvanSuikers\" AS NUMERIC) <= CAST(\"Koolhydraten\" AS NUMERIC)");
                    table.CheckConstraint("CK_Voedingswaarden_WaarvanVerzadigd", "\"WaarvanVerzadigd\" IS NULL OR CAST(\"Vetten\" AS NUMERIC) IS NULL OR CAST(\"WaarvanVerzadigd\" AS NUMERIC) <= CAST(\"Vetten\" AS NUMERIC)");
                    table.CheckConstraint("CK_Voedingswaarden_Zout", "\"Zout\" IS NULL OR \"Zout\" >= 0");
                    table.ForeignKey(
                        name: "FK_Voedingswaarden_Verspaketten_VerspakketId",
                        column: x => x.VerspakketId,
                        principalTable: "Verspaketten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerspakketAllergenen_VerspakketId_Allergeen",
                table: "VerspakketAllergenen",
                columns: new[] { "VerspakketId", "Allergeen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voedingswaarden_VerspakketId",
                table: "Voedingswaarden",
                column: "VerspakketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerspakketAllergenen");

            migrationBuilder.DropTable(
                name: "Voedingswaarden");
        }
    }
}
