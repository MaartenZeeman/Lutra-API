using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lutra.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class MakeVerspakketAggregateRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beoordelingen_Verspaketten_VerspakketId",
                table: "Beoordelingen");

            migrationBuilder.AlterColumn<Guid>(
                name: "VerspakketId",
                table: "Beoordelingen",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Beoordelingen_Verspaketten_VerspakketId",
                table: "Beoordelingen",
                column: "VerspakketId",
                principalTable: "Verspaketten",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beoordelingen_Verspaketten_VerspakketId",
                table: "Beoordelingen");

            migrationBuilder.AlterColumn<Guid>(
                name: "VerspakketId",
                table: "Beoordelingen",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Beoordelingen_Verspaketten_VerspakketId",
                table: "Beoordelingen",
                column: "VerspakketId",
                principalTable: "Verspaketten",
                principalColumn: "Id");
        }
    }
}
