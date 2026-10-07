using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AvanceFechaOrdenTerminada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "ACTUALIZADO_POR", "DESCRIPCION", "VALOR" },
                values: new object[] { "AVANCE.FECHA_ORDEN_TERMINADA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Avance por órdenes: una orden cerrada cuenta en la semana de su FIN_REAL (cuando terminó el trabajo) o de su CIERRE_TECNICO en SAP (decisión del 06/10/2026: fin real).", "FIN_REAL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.FECHA_ORDEN_TERMINADA");
        }
    }
}
