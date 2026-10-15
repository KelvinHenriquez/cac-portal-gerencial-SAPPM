using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActividadFinalYNotificable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NOTIFICABLE",
                table: "STG_SAP_OPERACION_PGPM",
                type: "NUMBER(1)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PARAMETRO_PGPM",
                columns: new[] { "CLAVE", "ACTUALIZADO_EN", "ACTUALIZADO_POR", "DESCRIPCION", "VALOR" },
                values: new object[] { "AVANCE.ACTIVIDAD_TERMINADA", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Avance por actividades: una actividad cuenta como terminada con horas reales Y la confirmación final de SAP (CONFIRMACION_FINAL, decisión del 15/10/2026) o con cualquier hora confirmada (HORAS). Cuenta en la semana de su última confirmación.", "CONFIRMACION_FINAL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO_PGPM",
                keyColumn: "CLAVE",
                keyValue: "AVANCE.ACTIVIDAD_TERMINADA");

            migrationBuilder.DropColumn(
                name: "NOTIFICABLE",
                table: "STG_SAP_OPERACION_PGPM");
        }
    }
}
