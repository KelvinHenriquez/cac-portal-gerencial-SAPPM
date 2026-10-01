using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParametrosEtapa3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "DESCRIPCION", "VALOR" },
                values: new object[,]
                {
                    { "AVANCE.FORMULA_REAL", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fórmula provisional del % real de avance (pendiente 'Total Ejec. %'): HH_ORDENES_CERRADAS u ORDENES_CERRADAS.", "HH_ORDENES_CERRADAS" },
                    { "SAP.FECHA_CARGA_INICIAL", new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fecha (yyyy-MM-dd) desde la que se extrae de SAP en la primera corrida.", "2026-01-01" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.FORMULA_REAL");

            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "SAP.FECHA_CARGA_INICIAL");
        }
    }
}
