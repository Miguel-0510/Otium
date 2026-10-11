using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGlosario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PalabrasGlosario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Palabra = table.Column<string>(type: "TEXT", nullable: false),
                    Definicion = table.Column<string>(type: "TEXT", nullable: false),
                    LibroTitulo = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PalabrasGlosario", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PalabrasGlosario");
        }
    }
}
