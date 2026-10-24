using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompartirEstantesYBloqueos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompartirEstantes",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Bloqueos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    BloqueadoId = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloqueos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloqueos_AspNetUsers_BloqueadoId",
                        column: x => x.BloqueadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bloqueos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueos_BloqueadoId",
                table: "Bloqueos",
                column: "BloqueadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueos_UsuarioId_BloqueadoId",
                table: "Bloqueos",
                columns: new[] { "UsuarioId", "BloqueadoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bloqueos");

            migrationBuilder.DropColumn(
                name: "CompartirEstantes",
                table: "AspNetUsers");
        }
    }
}
