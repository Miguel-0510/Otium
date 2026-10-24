using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otium.Data.Migrations
{
    /// <inheritdoc />
    public partial class AmistadClavePar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClavePar",
                table: "Amistades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Amistades"
                SET "ClavePar" = CASE
                    WHEN "SolicitanteId" < "ReceptorId"
                        THEN "SolicitanteId" || '|' || "ReceptorId"
                    ELSE "ReceptorId" || '|' || "SolicitanteId"
                END;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Amistades"
                WHERE "Id" NOT IN (
                    SELECT "Id" FROM (
                        SELECT "Id",
                               ROW_NUMBER() OVER (
                                   PARTITION BY "ClavePar"
                                   ORDER BY "Estado" DESC, "Id"
                               ) AS fila
                        FROM "Amistades"
                    )
                    WHERE fila = 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Amistades_ClavePar",
                table: "Amistades",
                column: "ClavePar",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Amistades_ClavePar",
                table: "Amistades");

            migrationBuilder.DropColumn(
                name: "ClavePar",
                table: "Amistades");
        }
    }
}
