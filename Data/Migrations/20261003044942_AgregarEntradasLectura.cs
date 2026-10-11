using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEntradasLectura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntradasLectura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LibroId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    PaginaInicio = table.Column<int>(type: "INTEGER", nullable: true),
                    PaginaFin = table.Column<int>(type: "INTEGER", nullable: true),
                    Resumen = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasLectura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradasLectura_Libros_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntradasLectura_LibroId",
                table: "EntradasLectura",
                column: "LibroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntradasLectura");
        }
    }
}
