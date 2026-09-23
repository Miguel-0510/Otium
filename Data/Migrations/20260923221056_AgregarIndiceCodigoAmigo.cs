using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceCodigoAmigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CodigoAmigo",
                table: "AspNetUsers",
                column: "CodigoAmigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CodigoAmigo",
                table: "AspNetUsers");
        }
    }
}
