using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnicoEntradaLectura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EntradasLectura_LibroId_Numero",
                table: "EntradasLectura",
                columns: new[] { "LibroId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EntradasLectura_LibroId_Numero",
                table: "EntradasLectura");
        }
    }
}
